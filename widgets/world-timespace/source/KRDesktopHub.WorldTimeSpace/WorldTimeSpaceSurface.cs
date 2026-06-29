using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using KRDesktopHub.WidgetSurface.Windows.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KRDesktopHub.WorldTimeSpace;

public sealed class WorldTimeSpaceSurfaceFactory : IWindowsWidgetSurfaceFactory
{
    public const string SurfaceWidgetId = "kr.world-time-space";

    public string WidgetId => SurfaceWidgetId;

    public ValueTask<IWindowsWidgetSurfaceLease> CreateSurfaceAsync(
        WindowsWidgetSurfaceMountContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(context.WidgetId, SurfaceWidgetId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("World Time-Space mount context widget id mismatch.");
        }

        var view = new WorldTimeSpaceView(context);
        return ValueTask.FromResult<IWindowsWidgetSurfaceLease>(new WorldTimeSpaceSurfaceLease(context, view));
    }
}

internal sealed class WorldTimeSpaceSurfaceLease : IWindowsWidgetSurfaceLease, IWindowsWidgetSurfaceHostStateSink, IAsyncDisposable
{
    private readonly WindowsWidgetSurfaceMountContext _context;
    private readonly WorldTimeSpaceView _view;
    private bool _disposed;

    public WorldTimeSpaceSurfaceLease(WindowsWidgetSurfaceMountContext context, WorldTimeSpaceView view)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        RootElement = view;
    }

    public string WidgetId => WorldTimeSpaceSurfaceFactory.SurfaceWidgetId;

    public FrameworkElement RootElement { get; }

    public void ApplyHostWidth(double hostWidthDip)
    {
        ThrowIfDisposed();
        _view.ApplyHostWidth(hostWidthDip);
    }

    public void ApplyCollapsed(bool collapsed)
    {
        ThrowIfDisposed();
        _view.ApplyCollapsed(collapsed);
    }

    public ValueTask PrepareForDetachAsync(WindowsWidgetSurfaceDetachContext context, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        _view.CloseTransientSurfaces();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _view.Dispose();
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

internal sealed class WorldTimeSpaceView : Border, IDisposable
{
    private const double OuterVerticalPaddingDip = 11.0;
    private const double HeaderHeightBudgetDip = 48.0;
    private const double CityGridTopMarginDip = 7.0;
    private const double CardRowHeightBudgetDip = 63.0;
    private const double InterRowGapBudgetDip = 9.0;
    private const double BottomSafetyBudgetDip = 6.0;
    private const double CardBottomPaddingDip = 3.0;
    private const double CollapsedHeightDip = 44.0;
    private const double MapExpandedHeightDip = 286.0;
    private const double MapPanelTopMarginDip = 6.0;
    private const double MapPanelHeightDip = 221.0;
    private const double MapPanelCornerRadiusDip = 0.0;
    private const double MapArtworkHeightDip = 230.0;
    private const int MapShadowBitmapWidth = 576;
    private const int MapShadowBitmapHeight = 230;
    private const double MapLonMin = -180.0;
    private const double MapLonMax = 180.0;
    private const double MapLatMin = -60.0;
    private const double MapLatMax = 84.0;
    private const int CardsPerRow = 4;
    private const int MaxCities = 16;
    private static readonly CultureInfo DisplayCulture = CultureInfo.InvariantCulture;
    private static readonly string StateDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRDesktopHub",
        "WorldTimeSpace");
    private static readonly string StatePath = Path.Combine(StateDirectory, "world-time-space-state-v4.json");
    private static readonly string[] DefaultVisibleCityIds =
    [
        "local",
        "los-angeles",
        "new-york",
        "buenos-aires",
        "lisbon",
        "johannesburg",
        "istanbul",
        "dubai",
        "ho-chi-minh-city",
        "hong-kong",
        "tokyo",
        "sydney"
    ];

    private readonly WindowsWidgetSurfaceMountContext _context;
    private readonly DispatcherTimer _timer;
    private readonly Grid _surfaceGrid;
    private readonly Grid _cityGrid;
    private readonly Border _mapPanel;
    private readonly Canvas _mapCanvas;
    private readonly Image _mapImage;
    private readonly Image _nightOverlay;
    private TextBlock _localZoneLabel = null!;
    private TextBlock _localClock = null!;
    private Slider _timeSlider = null!;
    private TextBlock _offsetLabel = null!;
    private readonly Popup _chooserPopup;
    private readonly TextBox _chooserSearch;
    private readonly StackPanel _chooserList;
    private readonly TextBlock _chooserLimit;
    private readonly IReadOnlyList<CityDefinition> _cityRegistry;
    private readonly List<string> _visibleCityIds;
    private readonly HolidayDataStore _holidayDataStore;
    private readonly CancellationTokenSource _lifetimeCts = new();

    private WidgetDisplayMode _displayMode = WidgetDisplayMode.Map;
    private bool _collapsed;
    private bool _disposed;
    private double _hostWidthDip;
    private Point? _dragStart;
    private string? _dragCityId;

    public WorldTimeSpaceView(WindowsWidgetSurfaceMountContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hostWidthDip = Math.Max(1.0, context.InitialHostWidthDip);
        _collapsed = context.Collapsed;
        _cityRegistry = CreateCityRegistry();
        _visibleCityIds = LoadVisibleCities(_cityRegistry);
        _holidayDataStore = new HolidayDataStore(ResolveCountryCodesForVisibleCities(_cityRegistry, _visibleCityIds), context.Network);

        Width = _hostWidthDip;
        MinHeight = CollapsedHeightDip;
        Height = _collapsed ? CollapsedHeightDip : DesiredExpandedHeightDip();
        Background = Brushes.White;
        Padding = new Thickness(9, 9, 9, 2);
        Focusable = true;

        _surfaceGrid = new Grid();
        _surfaceGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _surfaceGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Child = _surfaceGrid;

        var header = CreateHeader();
        Grid.SetRow(header, 0);
        _surfaceGrid.Children.Add(header);

        _cityGrid = new Grid { Margin = new Thickness(0, CityGridTopMarginDip, 0, 0) };
        for (var i = 0; i < CardsPerRow; i++)
        {
            _cityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        Grid.SetRow(_cityGrid, 1);
        _surfaceGrid.Children.Add(_cityGrid);

        _mapPanel = CreateMapPanel(out var mapCanvas, out var mapImage, out var nightOverlay);
        _mapCanvas = mapCanvas;
        _mapImage = mapImage;
        _nightOverlay = nightOverlay;
        Grid.SetRow(_mapPanel, 1);
        _surfaceGrid.Children.Add(_mapPanel);
        ApplyContentVisibility();

        _chooserSearch = new TextBox();
        _chooserList = new StackPanel();
        _chooserLimit = new TextBlock();
        _chooserPopup = CreateChooserPopup();
        ContextMenu = CreateRootContextMenu();
        KeyDown += OnKeyDown;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        Render();
        _ = RefreshHolidayDataAfterStartupAsync();
    }

    public void ApplyHostWidth(double hostWidthDip)
    {
        _hostWidthDip = Math.Max(1.0, hostWidthDip);
        Width = _hostWidthDip;
        Render();
    }

    public void ApplyCollapsed(bool collapsed)
    {
        _collapsed = collapsed;
        ApplyContentVisibility();
        Height = collapsed ? CollapsedHeightDip : DesiredExpandedHeightDip();
        ReportDesiredHeight();
    }

    public void CloseTransientSurfaces()
    {
        _chooserPopup.IsOpen = false;
        ContextMenu?.SetCurrentValue(ContextMenu.IsOpenProperty, false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCts.Cancel();
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _holidayDataStore.Dispose();
        _lifetimeCts.Dispose();
    }

    private static IReadOnlyList<CityDefinition> CreateCityRegistry()
    {
        var local = TimeZoneInfo.Local;
        var localIana = ResolveLocalIanaLikeId(local.Id);
        var localCity = new CityDefinition(
            "local",
            "Local",
            localIana,
            local.Id,
            ShortenZoneName(local.StandardName),
            ShortenZoneName(local.DaylightName),
            "LOCAL",
            Protected: true);

        return new[] { localCity }.Concat(BaseCityRegistry).ToArray();
    }

    private static IReadOnlyCollection<string> ResolveCountryCodesForVisibleCities(
        IReadOnlyList<CityDefinition> registry,
        IEnumerable<string> visibleCityIds)
    {
        var byId = registry.ToDictionary(city => city.Id, StringComparer.OrdinalIgnoreCase);
        return visibleCityIds
            .Select(id => byId.TryGetValue(id, out var city) ? city.CountryCode : null)
            .Where(countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(countryCode => countryCode!)
            .Where(countryCode => !string.Equals(countryCode, "LOCAL", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static readonly CityDefinition[] BaseCityRegistry =
    [
        new("lisbon", "Lisbon", "Europe/Lisbon", "GMT Standard Time", "WET", "WEST", "PT"),
        new("istanbul", "Istanbul", "Europe/Istanbul", "Turkey Standard Time", "TRT", "TRT", "TR"),
        new("hong-kong", "Hong Kong", "Asia/Hong_Kong", "China Standard Time", "HKT", "HKT", "HK"),
        new("vancouver", "Vancouver", "America/Vancouver", "Pacific Standard Time", "PST", "PDT", "CA"),
        new("new-york", "New York", "America/New_York", "Eastern Standard Time", "EST", "EDT", "US"),
        new("dubai", "Dubai", "Asia/Dubai", "Arabian Standard Time", "GST", "GST", "AE"),
        new("ho-chi-minh-city", "Ho Chi Minh City", "Asia/Ho_Chi_Minh", "SE Asia Standard Time", "ICT", "ICT", "VN"),
        new("hanoi", "Hanoi", "Asia/Ho_Chi_Minh", "SE Asia Standard Time", "ICT", "ICT", "VN"),
        new("bangkok", "Bangkok", "Asia/Bangkok", "SE Asia Standard Time", "ICT", "ICT", "TH"),
        new("singapore", "Singapore", "Asia/Singapore", "Singapore Standard Time", "SGT", "SGT", "SG"),
        new("shanghai", "Shanghai", "Asia/Shanghai", "China Standard Time", "CST", "CST", "CN"),
        new("beijing", "Beijing", "Asia/Shanghai", "China Standard Time", "CST", "CST", "CN"),
        new("shenzhen", "Shenzhen", "Asia/Shanghai", "China Standard Time", "CST", "CST", "CN"),
        new("tokyo", "Tokyo", "Asia/Tokyo", "Tokyo Standard Time", "JST", "JST", "JP"),
        new("seoul", "Seoul", "Asia/Seoul", "Korea Standard Time", "KST", "KST", "KR"),
        new("taipei", "Taipei", "Asia/Taipei", "Taipei Standard Time", "TST", "TST", "TW"),
        new("jakarta", "Jakarta", "Asia/Jakarta", "SE Asia Standard Time", "WIB", "WIB", "ID"),
        new("kuala-lumpur", "Kuala Lumpur", "Asia/Kuala_Lumpur", "Singapore Standard Time", "MYT", "MYT", "MY"),
        new("manila", "Manila", "Asia/Manila", "Singapore Standard Time", "PHT", "PHT", "PH"),
        new("delhi", "Delhi", "Asia/Kolkata", "India Standard Time", "IST", "IST", "IN"),
        new("mumbai", "Mumbai", "Asia/Kolkata", "India Standard Time", "IST", "IST", "IN"),
        new("sydney", "Sydney", "Australia/Sydney", "AUS Eastern Standard Time", "AEST", "AEDT", "AU"),
        new("melbourne", "Melbourne", "Australia/Melbourne", "AUS Eastern Standard Time", "AEST", "AEDT", "AU"),
        new("london", "London", "Europe/London", "GMT Standard Time", "GMT", "BST", "GB"),
        new("paris", "Paris", "Europe/Paris", "Romance Standard Time", "CET", "CEST", "FR"),
        new("berlin", "Berlin", "Europe/Berlin", "W. Europe Standard Time", "CET", "CEST", "DE"),
        new("madrid", "Madrid", "Europe/Madrid", "Romance Standard Time", "CET", "CEST", "ES"),
        new("rome", "Rome", "Europe/Rome", "W. Europe Standard Time", "CET", "CEST", "IT"),
        new("amsterdam", "Amsterdam", "Europe/Amsterdam", "W. Europe Standard Time", "CET", "CEST", "NL"),
        new("zurich", "Zurich", "Europe/Zurich", "W. Europe Standard Time", "CET", "CEST", "CH"),
        new("chicago", "Chicago", "America/Chicago", "Central Standard Time", "CST", "CDT", "US"),
        new("los-angeles", "Los Angeles", "America/Los_Angeles", "Pacific Standard Time", "PST", "PDT", "US"),
        new("san-francisco", "San Francisco", "America/Los_Angeles", "Pacific Standard Time", "PST", "PDT", "US"),
        new("toronto", "Toronto", "America/Toronto", "Eastern Standard Time", "EST", "EDT", "CA"),
        new("mexico-city", "Mexico City", "America/Mexico_City", "Central Standard Time (Mexico)", "CST", "CDT", "MX"),
        new("sao-paulo", "São Paulo", "America/Sao_Paulo", "E. South America Standard Time", "BRT", "BRT", "BR"),
        new("buenos-aires", "Buenos Aires", "America/Argentina/Buenos_Aires", "Argentina Standard Time", "ART", "ART", "AR"),
        new("santiago", "Santiago", "America/Santiago", "Pacific SA Standard Time", "CLT", "CLST", "CL"),
        new("cairo", "Cairo", "Africa/Cairo", "Egypt Standard Time", "EET", "EEST", "EG"),
        new("johannesburg", "Johannesburg", "Africa/Johannesburg", "South Africa Standard Time", "SAST", "SAST", "ZA"),
        new("cape-town", "Cape Town", "Africa/Johannesburg", "South Africa Standard Time", "SAST", "SAST", "ZA"),
        new("riyadh", "Riyadh", "Asia/Riyadh", "Arab Standard Time", "AST", "AST", "SA"),
        new("doha", "Doha", "Asia/Qatar", "Arab Standard Time", "AST", "AST", "QA"),
        new("tel-aviv", "Tel Aviv", "Asia/Jerusalem", "Israel Standard Time", "IST", "IDT", "IL"),
        new("athens", "Athens", "Europe/Athens", "GTB Standard Time", "EET", "EEST", "GR"),
        new("moscow", "Moscow", "Europe/Moscow", "Russian Standard Time", "MSK", "MSK", "RU"),
        new("warsaw", "Warsaw", "Europe/Warsaw", "Central European Standard Time", "CET", "CEST", "PL"),
        new("prague", "Prague", "Europe/Prague", "Central Europe Standard Time", "CET", "CEST", "CZ"),
        new("vienna", "Vienna", "Europe/Vienna", "W. Europe Standard Time", "CET", "CEST", "AT"),
        new("budapest", "Budapest", "Europe/Budapest", "Central Europe Standard Time", "CET", "CEST", "HU"),
        new("stockholm", "Stockholm", "Europe/Stockholm", "W. Europe Standard Time", "CET", "CEST", "SE"),
        new("oslo", "Oslo", "Europe/Oslo", "W. Europe Standard Time", "CET", "CEST", "NO"),
        new("copenhagen", "Copenhagen", "Europe/Copenhagen", "Romance Standard Time", "CET", "CEST", "DK"),
        new("brussels", "Brussels", "Europe/Brussels", "Romance Standard Time", "CET", "CEST", "BE"),
        new("dublin", "Dublin", "Europe/Dublin", "GMT Standard Time", "GMT", "IST", "IE"),
        new("helsinki", "Helsinki", "Europe/Helsinki", "FLE Standard Time", "EET", "EEST", "FI"),
        new("bucharest", "Bucharest", "Europe/Bucharest", "GTB Standard Time", "EET", "EEST", "RO"),
        new("sofia", "Sofia", "Europe/Sofia", "FLE Standard Time", "EET", "EEST", "BG"),
        new("belgrade", "Belgrade", "Europe/Belgrade", "Central Europe Standard Time", "CET", "CEST", "RS"),
        new("zagreb", "Zagreb", "Europe/Zagreb", "Central European Standard Time", "CET", "CEST", "HR"),
        new("kyiv", "Kyiv", "Europe/Kyiv", "FLE Standard Time", "EET", "EEST", "UA"),
        new("tbilisi", "Tbilisi", "Asia/Tbilisi", "Georgian Standard Time", "GET", "GET", "GE"),
        new("baku", "Baku", "Asia/Baku", "Azerbaijan Standard Time", "AZT", "AZT", "AZ"),
        new("yerevan", "Yerevan", "Asia/Yerevan", "Caucasus Standard Time", "AMT", "AMT", "AM"),
        new("tehran", "Tehran", "Asia/Tehran", "Iran Standard Time", "IRST", "IRDT", "IR"),
        new("kuwait-city", "Kuwait City", "Asia/Kuwait", "Arab Standard Time", "AST", "AST", "KW"),
        new("manama", "Manama", "Asia/Bahrain", "Arab Standard Time", "AST", "AST", "BH"),
        new("muscat", "Muscat", "Asia/Muscat", "Arabian Standard Time", "GST", "GST", "OM"),
        new("abu-dhabi", "Abu Dhabi", "Asia/Dubai", "Arabian Standard Time", "GST", "GST", "AE"),
        new("beirut", "Beirut", "Asia/Beirut", "Middle East Standard Time", "EET", "EEST", "LB"),
        new("amman", "Amman", "Asia/Amman", "Jordan Standard Time", "EET", "EEST", "JO"),
        new("baghdad", "Baghdad", "Asia/Baghdad", "Arabic Standard Time", "AST", "AST", "IQ"),
        new("karachi", "Karachi", "Asia/Karachi", "Pakistan Standard Time", "PKT", "PKT", "PK"),
        new("lahore", "Lahore", "Asia/Karachi", "Pakistan Standard Time", "PKT", "PKT", "PK"),
        new("dhaka", "Dhaka", "Asia/Dhaka", "Bangladesh Standard Time", "BST", "BST", "BD"),
        new("colombo", "Colombo", "Asia/Colombo", "Sri Lanka Standard Time", "SLST", "SLST", "LK"),
        new("kathmandu", "Kathmandu", "Asia/Kathmandu", "Nepal Standard Time", "NPT", "NPT", "NP"),
        new("yangon", "Yangon", "Asia/Yangon", "Myanmar Standard Time", "MMT", "MMT", "MM"),
        new("phnom-penh", "Phnom Penh", "Asia/Phnom_Penh", "SE Asia Standard Time", "ICT", "ICT", "KH"),
        new("vientiane", "Vientiane", "Asia/Vientiane", "SE Asia Standard Time", "ICT", "ICT", "LA"),
        new("osaka", "Osaka", "Asia/Tokyo", "Tokyo Standard Time", "JST", "JST", "JP"),
        new("fukuoka", "Fukuoka", "Asia/Tokyo", "Tokyo Standard Time", "JST", "JST", "JP"),
        new("macau", "Macau", "Asia/Macau", "China Standard Time", "CST", "CST", "MO"),
        new("auckland", "Auckland", "Pacific/Auckland", "New Zealand Standard Time", "NZST", "NZDT", "NZ"),
        new("brisbane", "Brisbane", "Australia/Brisbane", "E. Australia Standard Time", "AEST", "AEST", "AU"),
        new("perth", "Perth", "Australia/Perth", "W. Australia Standard Time", "AWST", "AWST", "AU"),
        new("miami", "Miami", "America/New_York", "Eastern Standard Time", "EST", "EDT", "US"),
        new("washington-dc", "Washington DC", "America/New_York", "Eastern Standard Time", "EST", "EDT", "US"),
        new("boston", "Boston", "America/New_York", "Eastern Standard Time", "EST", "EDT", "US"),
        new("seattle", "Seattle", "America/Los_Angeles", "Pacific Standard Time", "PST", "PDT", "US"),
        new("denver", "Denver", "America/Denver", "Mountain Standard Time", "MST", "MDT", "US"),
        new("dallas", "Dallas", "America/Chicago", "Central Standard Time", "CST", "CDT", "US"),
        new("houston", "Houston", "America/Chicago", "Central Standard Time", "CST", "CDT", "US"),
        new("montreal", "Montreal", "America/Toronto", "Eastern Standard Time", "EST", "EDT", "CA"),
        new("calgary", "Calgary", "America/Edmonton", "Mountain Standard Time", "MST", "MDT", "CA"),
        new("bogota", "Bogotá", "America/Bogota", "SA Pacific Standard Time", "COT", "COT", "CO"),
        new("medellin", "Medellín", "America/Bogota", "SA Pacific Standard Time", "COT", "COT", "CO"),
        new("lima", "Lima", "America/Lima", "SA Pacific Standard Time", "PET", "PET", "PE"),
        new("quito", "Quito", "America/Guayaquil", "SA Pacific Standard Time", "ECT", "ECT", "EC"),
        new("panama-city", "Panama City", "America/Panama", "SA Pacific Standard Time", "EST", "EST", "PA"),
        new("san-jose-costa-rica", "San José", "America/Costa_Rica", "Central America Standard Time", "CST", "CST", "CR"),
        new("guatemala-city", "Guatemala City", "America/Guatemala", "Central America Standard Time", "CST", "CST", "GT"),
        new("montevideo", "Montevideo", "America/Montevideo", "Montevideo Standard Time", "UYT", "UYT", "UY"),
        new("asuncion", "Asunción", "America/Asuncion", "Paraguay Standard Time", "PYT", "PYST", "PY"),
        new("caracas", "Caracas", "America/Caracas", "Venezuela Standard Time", "VET", "VET", "VE"),
        new("santo-domingo", "Santo Domingo", "America/Santo_Domingo", "SA Western Standard Time", "AST", "AST", "DO"),
        new("nairobi", "Nairobi", "Africa/Nairobi", "E. Africa Standard Time", "EAT", "EAT", "KE"),
        new("kigali", "Kigali", "Africa/Kigali", "South Africa Standard Time", "CAT", "CAT", "RW"),
        new("dar-es-salaam", "Dar es Salaam", "Africa/Dar_es_Salaam", "E. Africa Standard Time", "EAT", "EAT", "TZ"),
        new("mombasa", "Mombasa", "Africa/Nairobi", "E. Africa Standard Time", "EAT", "EAT", "KE"),
        new("lusaka", "Lusaka", "Africa/Lusaka", "South Africa Standard Time", "CAT", "CAT", "ZM"),
        new("casablanca", "Casablanca", "Africa/Casablanca", "Morocco Standard Time", "WET", "WEST", "MA"),
        new("lagos", "Lagos", "Africa/Lagos", "W. Central Africa Standard Time", "WAT", "WAT", "NG"),
        new("accra", "Accra", "Africa/Accra", "Greenwich Standard Time", "GMT", "GMT", "GH"),
        new("addis-ababa", "Addis Ababa", "Africa/Addis_Ababa", "E. Africa Standard Time", "EAT", "EAT", "ET"),
    ];

    private static string ResolveLocalIanaLikeId(string localWindowsOrIanaId)
    {
        return localWindowsOrIanaId switch
        {
            "Arabian Standard Time" => "Asia/Dubai",
            "Turkey Standard Time" => "Europe/Istanbul",
            "GMT Standard Time" => "Europe/Lisbon",
            "China Standard Time" => "Asia/Hong_Kong",
            "Pacific Standard Time" => "America/Los_Angeles",
            "Eastern Standard Time" => "America/New_York",
            _ => localWindowsOrIanaId
        };
    }

    private static string ShortenZoneName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var letters = new string(name.Where(char.IsUpper).Take(4).ToArray());
        if (!string.IsNullOrWhiteSpace(letters))
        {
            return letters;
        }

        return string.Join(string.Empty, name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => part[0])).ToUpperInvariant();
    }

    private Border CreateHeader()
    {
        var border = new Border
        {
            BorderBrush = BrushFrom("#D8DEE7"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Background = BrushFrom("#F5F7FA"),
            Padding = new Thickness(8, 6, 8, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        border.Child = grid;

        var titleStack = new StackPanel
        {
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            ToolTip = "Double-click to switch map/list"
        };
        titleStack.MouseLeftButtonDown += OnTitleMouseLeftButtonDown;
        titleStack.Children.Add(new TextBlock
        {
            Text = "World Time-Space",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = BrushFrom("#18212B")
        });
        var localLine = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 0)
        };
        _localZoneLabel = new TextBlock
        {
            Text = "Local · ",
            FontSize = 10,
            Foreground = BrushFrom("#647180")
        };
        _localClock = new TextBlock
        {
            Text = "--:--",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = BrushFrom("#C46A21")
        };
        localLine.Children.Add(_localZoneLabel);
        localLine.Children.Add(_localClock);
        titleStack.Children.Add(localLine);
        Grid.SetColumn(titleStack, 0);
        grid.Children.Add(titleStack);

        var sliderGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
        sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _timeSlider = new Slider
        {
            Minimum = -24,
            Maximum = 24,
            Value = 0,
            TickFrequency = 0.5,
            IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 0, 6, 0)
        };
        _timeSlider.ValueChanged += (_, _) => Render();
        Grid.SetColumn(_timeSlider, 0);
        sliderGrid.Children.Add(_timeSlider);

        _offsetLabel = new TextBlock
        {
            MinWidth = 38,
            Text = "Now",
            FontSize = 10,
            Foreground = BrushFrom("#647180"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        Grid.SetColumn(_offsetLabel, 1);
        sliderGrid.Children.Add(_offsetLabel);
        Grid.SetColumn(sliderGrid, 1);
        grid.Children.Add(sliderGrid);

        var reset = new Button
        {
            Content = "Now",
            FontSize = 11,
            Padding = new Thickness(7, 5, 7, 5),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        reset.Click += (_, _) => _timeSlider.Value = 0;
        Grid.SetColumn(reset, 2);
        grid.Children.Add(reset);

        return border;
    }

    private Border CreateMapPanel(out Canvas canvas, out Image mapImage, out Image nightOverlay)
    {
        var border = new Border
        {
            Height = MapPanelHeightDip,
            Margin = new Thickness(0, MapPanelTopMarginDip, 0, 0),
            BorderBrush = BrushFrom("#CBD3DA"),
            BorderThickness = new Thickness(1, 1, 1, 0),
            CornerRadius = new CornerRadius(MapPanelCornerRadiusDip),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Background = BrushFrom("#EDF4F8"),
            ClipToBounds = true,
            ContextMenu = CreateRootContextMenu()
        };

        canvas = new Canvas
        {
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Background = BrushFrom("#EDF4F8")
        };

        mapImage = new Image
        {
            Stretch = Stretch.Fill,
            Source = LoadMapImageSource()
        };
        Canvas.SetZIndex(mapImage, 0);
        canvas.Children.Add(mapImage);

        nightOverlay = new Image
        {
            Stretch = Stretch.Fill,
            Opacity = 0.86,
            IsHitTestVisible = false
        };
        Canvas.SetZIndex(nightOverlay, 1);
        canvas.Children.Add(nightOverlay);

        border.Child = canvas;
        return border;
    }

    private static ImageSource? LoadMapImageSource()
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri("pack://application:,,,/KRDesktopHub.WorldTimeSpace;component/Assets/world_map_precise_lat84_-60.png", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2)
        {
            return;
        }

        _displayMode = _displayMode == WidgetDisplayMode.Map ? WidgetDisplayMode.List : WidgetDisplayMode.Map;
        ApplyContentVisibility();
        Render();
        e.Handled = true;
    }

    private void ApplyContentVisibility()
    {
        if (_collapsed)
        {
            _cityGrid.Visibility = Visibility.Collapsed;
            _mapPanel.Visibility = Visibility.Collapsed;
            return;
        }

        _cityGrid.Visibility = _displayMode == WidgetDisplayMode.List ? Visibility.Visible : Visibility.Collapsed;
        _mapPanel.Visibility = _displayMode == WidgetDisplayMode.Map ? Visibility.Visible : Visibility.Collapsed;
    }

    private ContextMenu CreateRootContextMenu()
    {
        var menu = new ContextMenu();
        var add = new MenuItem { Header = "Add city..." };
        add.Click += (_, _) => OpenChooser();
        menu.Items.Add(add);
        return menu;
    }

    private ContextMenu CreateCardContextMenu(CityDefinition city)
    {
        var menu = new ContextMenu();
        var add = new MenuItem { Header = "Add city..." };
        add.Click += (_, _) => OpenChooser();
        menu.Items.Add(add);

        if (!city.Protected)
        {
            var remove = new MenuItem { Header = "Remove city" };
            remove.Click += (_, _) => RemoveCity(city.Id);
            menu.Items.Add(remove);
        }

        return menu;
    }

    private Popup CreateChooserPopup()
    {
        var popup = new Popup
        {
            AllowsTransparency = true,
            StaysOpen = false,
            Placement = PlacementMode.Relative,
            PlacementTarget = this,
            HorizontalOffset = 80,
            VerticalOffset = 24,
            PopupAnimation = PopupAnimation.Fade
        };

        var border = new Border
        {
            Width = 500,
            MaxHeight = 540,
            BorderBrush = BrushFrom("#D8DEE7"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10),
            Background = Brushes.White,
            Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 4, Opacity = 0.22 }
        };

        var stack = new StackPanel();
        border.Child = stack;

        var head = new DockPanel();
        var close = new Button { Content = "Close", FontSize = 11, Padding = new Thickness(7, 4, 7, 4) };
        close.Click += (_, _) => popup.IsOpen = false;
        DockPanel.SetDock(close, Dock.Right);
        head.Children.Add(close);
        head.Children.Add(new TextBlock
        {
            Text = "Add city",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(head);

        _chooserSearch.Margin = new Thickness(0, 9, 0, 7);
        _chooserSearch.FontSize = 12;
        _chooserSearch.Padding = new Thickness(8, 6, 8, 6);
        _chooserSearch.MinWidth = 330;
        _chooserSearch.TextChanged += (_, _) => RenderChooserList();
        stack.Children.Add(_chooserSearch);

        var scroll = new ScrollViewer
        {
            MaxHeight = 420,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _chooserList
        };
        stack.Children.Add(scroll);

        _chooserLimit.Margin = new Thickness(0, 7, 0, 0);
        _chooserLimit.FontSize = 9;
        _chooserLimit.Foreground = BrushFrom("#8793A1");
        stack.Children.Add(_chooserLimit);

        popup.Child = border;
        return popup;
    }

    private void OpenChooser()
    {
        _chooserSearch.Text = string.Empty;
        RenderChooserList();
        _chooserPopup.IsOpen = true;
        _chooserSearch.Focus();
    }

    private void RenderChooserList()
    {
        _chooserList.Children.Clear();
        var query = _chooserSearch.Text.Trim();
        var atCapacity = _visibleCityIds.Count >= MaxCities;
        var available = _cityRegistry
            .Where(city => !city.Protected)
            .Where(city => !_visibleCityIds.Contains(city.Id, StringComparer.OrdinalIgnoreCase))
            .Where(city => string.IsNullOrWhiteSpace(query)
                || city.City.Contains(query, StringComparison.OrdinalIgnoreCase)
                || city.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                || city.IanaZone.Contains(query, StringComparison.OrdinalIgnoreCase)
                || city.CountryCode.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (!atCapacity)
        {
            foreach (var city in available)
            {
                var button = new Button
                {
                    Padding = new Thickness(8, 7, 8, 7),
                    Margin = new Thickness(0, 0, 0, 3),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = city.City, FontSize = 12 });
                stack.Children.Add(new TextBlock
                {
                    Text = $"{city.IanaZone} · {city.CountryCode}",
                    FontSize = 9,
                    Foreground = BrushFrom("#8793A1"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
                button.Content = stack;
                button.Click += (_, _) => AddCity(city.Id);
                _chooserList.Children.Add(button);
            }
        }

        _chooserLimit.Text = atCapacity
            ? $"Maximum reached: {_visibleCityIds.Count} / {MaxCities} cities"
            : $"{_visibleCityIds.Count} / {MaxCities} cities";
    }

    private void AddCity(string cityId)
    {
        if (_visibleCityIds.Count >= MaxCities)
        {
            return;
        }

        if (_visibleCityIds.Contains(cityId, StringComparer.OrdinalIgnoreCase) || FindCity(cityId) is null)
        {
            return;
        }

        _visibleCityIds.Add(cityId);
        var city = FindCity(cityId);
        if (city is not null)
        {
            _holidayDataStore.AddCountryCode(city.CountryCode);
        }
        SaveVisibleCities();
        Render();
        RenderChooserList();
    }

    private void RemoveCity(string cityId)
    {
        var city = FindCity(cityId);
        if (city is null || city.Protected)
        {
            return;
        }

        _visibleCityIds.RemoveAll(id => string.Equals(id, cityId, StringComparison.OrdinalIgnoreCase));
        SaveVisibleCities();
        Render();
        RenderChooserList();
    }

    private void Render()
    {
        if (_disposed)
        {
            return;
        }

        var shift = _timeSlider.Value;
        _offsetLabel.Text = Math.Abs(shift) >= 0.001 ? $"{(shift > 0 ? "+" : string.Empty)}{shift:0.#}h" : "Now";
        RenderContent();
        Height = _collapsed ? CollapsedHeightDip : DesiredExpandedHeightDip();
        ReportDesiredHeight();
    }

    private void RenderContent()
    {
        var utcNow = SimulatedUtcNow();
        UpdateLocalHeader(utcNow);

        if (_displayMode == WidgetDisplayMode.Map)
        {
            RenderMap(utcNow);
        }
        else
        {
            RenderCities(utcNow);
        }
    }

    private void UpdateLocalHeader(DateTimeOffset utcNow)
    {
        var localCity = FindCity("local") ?? _cityRegistry[0];
        var localInfo = BuildTimeInfo(utcNow, localCity);
        _localZoneLabel.Text = $"{localCity.IanaZone} · ";
        _localClock.Text = localInfo.HourMinute;
    }

    private void RenderCities(DateTimeOffset utcNow)
    {
        _cityGrid.Children.Clear();
        _cityGrid.RowDefinitions.Clear();

        var rows = VisibleRows();
        for (var i = 0; i < rows; i++)
        {
            _cityGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var index = 0; index < _visibleCityIds.Count && index < MaxCities; index++)
        {
            var city = FindCity(_visibleCityIds[index]);
            if (city is null)
            {
                continue;
            }

            var card = CreateCityCard(city, utcNow);
            Grid.SetColumn(card, index % CardsPerRow);
            Grid.SetRow(card, index / CardsPerRow);
            _cityGrid.Children.Add(card);
        }
    }

    private void RenderMap(DateTimeOffset utcNow)
    {
        _mapCanvas.Children.Clear();

        var viewportWidth = Math.Max(1.0, _mapCanvas.ActualWidth > 0 ? _mapCanvas.ActualWidth : Math.Max(1.0, _hostWidthDip - 20.0));
        var viewportHeight = Math.Max(1.0, _mapCanvas.ActualHeight > 0 ? _mapCanvas.ActualHeight : Math.Max(1.0, MapPanelHeightDip - 2.0));

        // Keep the accepted 230-DIP map artwork scale, but show it through a shorter visible viewport.
        // The vertical crop is centered, so a small amount is cropped from the top and bottom instead
        // of cutting only the south edge of the map.
        var artworkHeight = Math.Max(viewportHeight, MapArtworkHeightDip);
        var artworkTop = (viewportHeight - artworkHeight) / 2.0;

        _mapImage.Width = viewportWidth;
        _mapImage.Height = artworkHeight;
        Canvas.SetLeft(_mapImage, 0);
        Canvas.SetTop(_mapImage, artworkTop);
        Canvas.SetZIndex(_mapImage, 0);
        _mapCanvas.Children.Add(_mapImage);

        _nightOverlay.Width = viewportWidth;
        _nightOverlay.Height = artworkHeight;
        _nightOverlay.Source = CreateNightShadowBitmap(utcNow);
        Canvas.SetLeft(_nightOverlay, 0);
        Canvas.SetTop(_nightOverlay, artworkTop);
        Canvas.SetZIndex(_nightOverlay, 1);
        _mapCanvas.Children.Add(_nightOverlay);

        foreach (var cityId in _visibleCityIds.Take(MaxCities))
        {
            var city = FindCity(cityId);
            if (city is null || !MapCityLayouts.TryGetValue(city.Id, out var layout))
            {
                continue;
            }

            AddMapCityMarker(city, layout, utcNow, viewportWidth, artworkHeight, artworkTop);
        }

        AddMapBottomBorderLine(viewportWidth, viewportHeight);
    }

    private void AddMapBottomBorderLine(double viewportWidth, double viewportHeight)
    {
        var bottomY = Math.Max(0.5, viewportHeight - 0.5);
        var bottomLine = new System.Windows.Shapes.Line
        {
            X1 = 0,
            Y1 = bottomY,
            X2 = viewportWidth,
            Y2 = bottomY,
            Stroke = BrushFrom("#CBD3DA"),
            StrokeThickness = 1.0,
            StrokeStartLineCap = PenLineCap.Flat,
            StrokeEndLineCap = PenLineCap.Flat,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Canvas.SetZIndex(bottomLine, 20);
        _mapCanvas.Children.Add(bottomLine);
    }

    private void AddMapCityMarker(CityDefinition city, MapCityLayout layout, DateTimeOffset utcNow, double mapWidth, double artworkHeight, double artworkTop)
    {
        var info = BuildTimeInfo(utcNow, city);
        var x = ProjectMapX(layout.Longitude, mapWidth);
        var y = artworkTop + ProjectMapY(layout.Latitude, artworkHeight);
        var labelX = x + layout.Dx;
        var labelY = y + layout.Dy - 17.0;
        var anchorX = x + layout.Dx;
        var anchorY = y + layout.Dy;

        var leader = new System.Windows.Shapes.Line
        {
            X1 = x + 5.0,
            Y1 = y + 5.0,
            X2 = anchorX,
            Y2 = anchorY,
            Stroke = BrushFrom("#66717C"),
            StrokeThickness = 1.0,
            Opacity = 0.42,
            IsHitTestVisible = false
        };
        Canvas.SetZIndex(leader, 2);
        _mapCanvas.Children.Add(leader);

        var dot = new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = MarkerFill(info.Status),
            Stroke = Brushes.White,
            StrokeThickness = 1,
            Effect = new DropShadowEffect { BlurRadius = 3, ShadowDepth = 1, Opacity = 0.22 }
        };
        Canvas.SetLeft(dot, x - 5.0);
        Canvas.SetTop(dot, y - 5.0);
        Canvas.SetZIndex(dot, 4);
        _mapCanvas.Children.Add(dot);

        var label = new StackPanel
        {
            Width = layout.SmallLabel ? 48.0 : 50.0,
            ToolTip = BuildCardToolTip(city, info)
        };
        label.Children.Add(new TextBlock
        {
            Text = layout.Code,
            FontSize = layout.SmallLabel ? 13 : 15,
            LineHeight = layout.SmallLabel ? 13 : 15,
            FontWeight = FontWeights.ExtraBold,
            Foreground = BrushFrom("#14202B"),
            TextAlignment = layout.LabelLeft ? TextAlignment.Right : TextAlignment.Left
        });
        label.Children.Add(new TextBlock
        {
            Text = info.HourMinute,
            FontSize = layout.SmallLabel ? 13 : 14,
            LineHeight = layout.SmallLabel ? 13 : 14,
            Foreground = BrushFrom("#14202B"),
            TextAlignment = layout.LabelLeft ? TextAlignment.Right : TextAlignment.Left
        });
        label.Effect = new DropShadowEffect
        {
            Color = Colors.White,
            BlurRadius = 3,
            ShadowDepth = 0,
            Opacity = 0.85
        };
        Canvas.SetLeft(label, labelX);
        Canvas.SetTop(label, labelY);
        Canvas.SetZIndex(label, 5);
        _mapCanvas.Children.Add(label);
    }

    private static Brush MarkerFill(CityDayStatus status) => status switch
    {
        CityDayStatus.WorkingHours => BrushFrom("#2E9225"),
        CityDayStatus.WorkdayOffHours => BrushFrom("#147FE3"),
        _ => BrushFrom("#F5F7F9")
    };

    private static double ProjectMapX(double longitude, double mapWidth) => (longitude - MapLonMin) / (MapLonMax - MapLonMin) * mapWidth;

    private static double ProjectMapY(double latitude, double mapHeight) => (MapLatMax - latitude) / (MapLatMax - MapLatMin) * mapHeight;

    private static BitmapSource CreateNightShadowBitmap(DateTimeOffset utcNow)
    {
        var width = MapShadowBitmapWidth;
        var height = MapShadowBitmapHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        var (declination, subsolarLongitude) = SolarPosition(utcNow);

        for (var py = 0; py < height; py++)
        {
            var latitude = DegreesToRadians(MapLatMax - ((py + 0.5) / height) * (MapLatMax - MapLatMin));
            var sinLatitude = Math.Sin(latitude);
            var cosLatitude = Math.Cos(latitude);

            for (var px = 0; px < width; px++)
            {
                var longitude = DegreesToRadians(MapLonMin + ((px + 0.5) / width) * (MapLonMax - MapLonMin));
                var cosZenith = sinLatitude * Math.Sin(declination)
                    + cosLatitude * Math.Cos(declination) * Math.Cos(longitude - subsolarLongitude);
                var altitude = RadiansToDegrees(Math.Asin(Math.Clamp(cosZenith, -1.0, 1.0)));
                var alpha = NightAlpha(altitude);
                var offset = py * stride + px * 4;
                pixels[offset] = 88;
                pixels[offset + 1] = 78;
                pixels[offset + 2] = 70;
                pixels[offset + 3] = alpha;
            }
        }

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte NightAlpha(double solarAltitudeDegrees)
    {
        if (solarAltitudeDegrees >= 0)
        {
            return 0;
        }

        if (solarAltitudeDegrees > -8)
        {
            return (byte)Math.Round(Math.Pow(-solarAltitudeDegrees / 8.0, 1.35) * 86.0);
        }

        return solarAltitudeDegrees < -18 ? (byte)98 : (byte)86;
    }

    private static (double DeclinationRadians, double SubsolarLongitudeRadians) SolarPosition(DateTimeOffset utcNow)
    {
        var julianDay = utcNow.ToUnixTimeMilliseconds() / 86400000.0 + 2440587.5;
        var n = julianDay - 2451545.0;
        var meanLongitude = NormalizeDegrees(280.460 + 0.9856474 * n);
        var meanAnomaly = DegreesToRadians(NormalizeDegrees(357.528 + 0.9856003 * n));
        var eclipticLongitude = DegreesToRadians(NormalizeDegrees(meanLongitude + 1.915 * Math.Sin(meanAnomaly) + 0.020 * Math.Sin(2 * meanAnomaly)));
        var obliquity = DegreesToRadians(23.439 - 0.0000004 * n);
        var declination = Math.Asin(Math.Sin(obliquity) * Math.Sin(eclipticLongitude));
        var rightAscension = NormalizeDegrees(RadiansToDegrees(Math.Atan2(Math.Cos(obliquity) * Math.Sin(eclipticLongitude), Math.Cos(eclipticLongitude))));
        var t = (julianDay - 2451545.0) / 36525.0;
        var greenwichSidereal = NormalizeDegrees(280.46061837 + 360.98564736629 * (julianDay - 2451545.0) + 0.000387933 * t * t - t * t * t / 38710000.0);
        var subsolarLongitude = NormalizeSignedDegrees(rightAscension - greenwichSidereal);
        return (declination, DegreesToRadians(subsolarLongitude));
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private static double NormalizeSignedDegrees(double value)
    {
        var normalized = NormalizeDegrees(value + 180.0) - 180.0;
        return normalized;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    private Border CreateCityCard(CityDefinition city, DateTimeOffset utcNow)
    {
        var info = BuildTimeInfo(utcNow, city);
        var card = new Border
        {
            MinHeight = 45,
            BorderBrush = BrushFrom("#D8DEE7"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Background = BrushFrom("#F5F7FA"),
            Padding = new Thickness(8, 6, 8, CardBottomPaddingDip),
            Margin = new Thickness(3, 0, 3, 4),
            ContextMenu = CreateCardContextMenu(city),
            Tag = city.Id,
            AllowDrop = true,
            ToolTip = BuildCardToolTip(city, info)
        };

        card.PreviewMouseLeftButtonDown += OnCardMouseLeftButtonDown;
        card.PreviewMouseMove += OnCardMouseMove;
        card.Drop += OnCardDrop;
        card.DragOver += OnCardDragOver;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        card.Child = grid;

        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = city.City,
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = BrushFrom("#647180"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(title, 0);
        titleGrid.Children.Add(title);

        var badge = CreateStatusBadge(info.Status);
        Grid.SetColumn(badge, 1);
        titleGrid.Children.Add(badge);
        Grid.SetRow(titleGrid, 0);
        grid.Children.Add(titleGrid);

        var time = new TextBlock
        {
            Text = info.HourMinute,
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = BrushFrom(city.Id == "local" ? "#C46A21" : "#18212B"),
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetRow(time, 1);
        grid.Children.Add(time);

        var footer = new Grid { Margin = new Thickness(0, 3, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var date = new TextBlock
        {
            Text = info.WeekdayDate,
            FontSize = 8,
            Foreground = BrushFrom("#8793A1"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(date, 0);
        footer.Children.Add(date);

        var zone = new TextBlock
        {
            Text = info.ZoneAbbreviation,
            FontSize = 8,
            Foreground = BrushFrom("#8793A1"),
            TextAlignment = TextAlignment.Right,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(zone, 1);
        footer.Children.Add(zone);
        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        return card;
    }

    private static Border CreateStatusBadge(CityDayStatus status)
    {
        var (text, border, background, foreground) = status switch
        {
            CityDayStatus.WorkingHours => ("WORK", "#A7D7B5", "#EAF7EF", "#167A3A"),
            CityDayStatus.WorkdayOffHours => ("OFF", "#B8CBEF", "#EEF4FF", "#285AA8"),
            CityDayStatus.Holiday => ("HOL", "#D8DEE7", "#EEF2F6", "#647180"),
            _ => ("—", "#D8DEE7", "#EEF2F6", "#8793A1")
        };

        return new Border
        {
            MinWidth = 24,
            Height = 14,
            Padding = new Thickness(4, 0, 4, 0),
            CornerRadius = new CornerRadius(7),
            BorderThickness = new Thickness(1),
            BorderBrush = BrushFrom(border),
            Background = BrushFrom(background),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFrom(foreground),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static readonly IReadOnlyDictionary<string, MapCityLayout> MapCityLayouts = new Dictionary<string, MapCityLayout>(StringComparer.OrdinalIgnoreCase)
    {
        ["los-angeles"] = new("LAX", 34.0522, -118.2437, 10, 0, LabelLeft: false),
        ["new-york"] = new("NYC", 40.7128, -74.0060, 10, -2, LabelLeft: false),
        ["buenos-aires"] = new("BA", -34.6037, -58.3816, 10, 0, LabelLeft: false),
        ["lisbon"] = new("LIS", 38.7223, -9.1393, -48, -5, LabelLeft: true),
        ["johannesburg"] = new("JNB", -26.2041, 28.0473, 10, 0, LabelLeft: false),
        ["istanbul"] = new("IST", 41.0082, 28.9784, 10, -13, LabelLeft: false),
        ["dubai"] = new("DXB", 25.2048, 55.2708, 12, 10, LabelLeft: false),
        ["ho-chi-minh-city"] = new("HCMC", 10.8231, 106.6297, -44, 34, LabelLeft: true, SmallLabel: true),
        ["hong-kong"] = new("HKG", 22.3193, 114.1694, -34, -28, LabelLeft: true),
        ["tokyo"] = new("TYO", 35.6762, 139.6503, -18, -20, LabelLeft: false),
        ["sydney"] = new("SYD", -33.8688, 151.2093, -18, -18, LabelLeft: false)
    };

    private string BuildCardToolTip(CityDefinition city, CityTimeInfo info)
    {
        var status = info.Status switch
        {
            CityDayStatus.WorkingHours => "Legal workday, 09:00–18:00 local time",
            CityDayStatus.WorkdayOffHours => "Legal workday, outside 09:00–18:00 local time",
            CityDayStatus.Holiday => "Local statutory holiday",
            _ => "Weekend or rest day"
        };
        return $"{city.City} · {city.IanaZone} · {status}";
    }

    private CityTimeInfo BuildTimeInfo(DateTimeOffset utcNow, CityDefinition city)
    {
        var timeZone = city.ResolveTimeZone();
        var localDateTime = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var dateKey = localDateTime.ToString("yyyy-MM-dd", DisplayCulture);
        var zone = city.ZoneAbbreviation(localDateTime.DateTime, timeZone);
        var isWeekend = localDateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isHoliday = city.CountryCode != "LOCAL" && _holidayDataStore.IsHoliday(city.CountryCode, dateKey);
        var isWorkday = !isWeekend && !isHoliday;
        var isWorkingHours = isWorkday && localDateTime.TimeOfDay >= TimeSpan.FromHours(9) && localDateTime.TimeOfDay < TimeSpan.FromHours(18);

        var status = isHoliday
            ? CityDayStatus.Holiday
            : isWorkingHours
                ? CityDayStatus.WorkingHours
                : isWorkday
                    ? CityDayStatus.WorkdayOffHours
                    : CityDayStatus.RestDay;

        return new CityTimeInfo(
            localDateTime.ToString("HH:mm", DisplayCulture),
            localDateTime.ToString("ddd dd/MM", DisplayCulture),
            zone,
            status);
    }

    private DateTimeOffset SimulatedUtcNow() => DateTimeOffset.UtcNow.AddHours(_timeSlider.Value);

    private CityDefinition? FindCity(string cityId) => _cityRegistry.FirstOrDefault(city => string.Equals(city.Id, cityId, StringComparison.OrdinalIgnoreCase));

    private int VisibleRows()
    {
        var rows = Math.Max(1, (int)Math.Ceiling(Math.Min(_visibleCityIds.Count, MaxCities) / (double)CardsPerRow));
        return Math.Min(4, rows);
    }

    private double DesiredExpandedHeightDip()
    {
        var rows = VisibleRows();
        var listHeight = OuterVerticalPaddingDip
            + HeaderHeightBudgetDip
            + CityGridTopMarginDip
            + rows * CardRowHeightBudgetDip
            + Math.Max(0, rows - 1) * InterRowGapBudgetDip
            + BottomSafetyBudgetDip;

        return Math.Max(MapExpandedHeightDip, listHeight);
    }

    private void ReportDesiredHeight()
    {
        var desired = _collapsed ? CollapsedHeightDip : DesiredExpandedHeightDip();
        _context.DesiredHeightSink.ReportDesiredHeight(_context.WidgetId, desired);
    }

    private void OnTimerTick(object? sender, EventArgs e) => Render();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseTransientSurfaces();
        }
    }

    private void OnCardMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border card && card.Tag is string cityId)
        {
            _dragStart = e.GetPosition(this);
            _dragCityId = cityId;
        }
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is null || _dragCityId is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStart.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(this, _dragCityId, DragDropEffects.Move);
        _dragStart = null;
        _dragCityId = null;
    }

    private void OnCardDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(string)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnCardDrop(object sender, DragEventArgs e)
    {
        if (sender is not Border target || target.Tag is not string targetId || e.Data.GetData(typeof(string)) is not string sourceId)
        {
            return;
        }

        MoveCity(sourceId, targetId);
        e.Handled = true;
    }

    private void MoveCity(string sourceId, string targetId)
    {
        if (string.Equals(sourceId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sourceIndex = _visibleCityIds.FindIndex(id => string.Equals(id, sourceId, StringComparison.OrdinalIgnoreCase));
        var targetIndex = _visibleCityIds.FindIndex(id => string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase));
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }

        var item = _visibleCityIds[sourceIndex];
        _visibleCityIds.RemoveAt(sourceIndex);
        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }
        _visibleCityIds.Insert(targetIndex, item);
        SaveVisibleCities();
        Render();
    }

    private async Task RefreshHolidayDataAfterStartupAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8), _lifetimeCts.Token).ConfigureAwait(false);
            await _holidayDataStore.RefreshOnceAsync(_lifetimeCts.Token).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(Render, DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
            // Detach/dispose cancels background refresh.
        }
        catch
        {
            // Startup local holiday cache refresh must never break the widget UI. Existing cache or seeded fallback remains active.
        }
    }

    private List<string> LoadVisibleCities(IReadOnlyList<CityDefinition> registry)
    {
        var fallback = DefaultVisibleCityIds.ToList();
        try
        {
            if (!File.Exists(StatePath))
            {
                return fallback;
            }

            var json = File.ReadAllText(StatePath);
            var state = JsonSerializer.Deserialize<WidgetState>(json);
            if (state?.VisibleCityIds is null || state.VisibleCityIds.Length == 0)
            {
                return fallback;
            }

            var valid = registry.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ordered = state.VisibleCityIds
                .Where(id => valid.Contains(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxCities)
                .ToList();

            if (!ordered.Contains("local", StringComparer.OrdinalIgnoreCase))
            {
                ordered.Insert(0, "local");
            }

            return ordered.Count > 0 ? ordered.Take(MaxCities).ToList() : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void SaveVisibleCities()
    {
        try
        {
            Directory.CreateDirectory(StateDirectory);
            var state = new WidgetState(_visibleCityIds.Take(MaxCities).ToArray());
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            var tmp = StatePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, StatePath, overwrite: true);
        }
        catch
        {
            // State persistence is best effort; UI should remain responsive if local storage is locked.
        }
    }

    private static Brush BrushFrom(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }

    private sealed record WidgetState(string[] VisibleCityIds);
}

internal sealed class HolidayDataStore : IDisposable
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRDesktopHub",
        "WorldTimeSpace");
    private static readonly string CachePath = Path.Combine(DirectoryPath, "holiday-cache-v3.json");
    private readonly object _countryLock = new();
    private readonly HashSet<string> _countryCodes;
    private readonly IWindowsWidgetNetworkReadBroker? _network;
    private HolidayCache _cache;
    private static int s_refreshAttempted;

    public HolidayDataStore(IEnumerable<string> countryCodes, IWindowsWidgetNetworkReadBroker? network)
    {
        _countryCodes = countryCodes
            .Where(c => !string.Equals(c, "LOCAL", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _network = network;
        _cache = LoadCache();
    }

    public void AddCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || string.Equals(countryCode, "LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lock (_countryLock)
        {
            _countryCodes.Add(countryCode);
        }
    }

    public bool IsHoliday(string countryCode, string yyyyMmDd)
    {
        if (_cache.Holidays.TryGetValue(countryCode, out var dates) && dates.Contains(yyyyMmDd))
        {
            return true;
        }

        return SeededHolidayFallback.IsHoliday(countryCode, yyyyMmDd);
    }

    public async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_network is null)
        {
            return;
        }

        if (Interlocked.Exchange(ref s_refreshAttempted, 1) == 1)
        {
            return;
        }

        string[] countryCodes;
        lock (_countryLock)
        {
            countryCodes = _countryCodes.OrderBy(countryCode => countryCode, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var currentYear = DateTime.UtcNow.Year;
        var years = new[] { currentYear, currentYear + 1 };
        var next = SeededHolidayFallback.CreateCache(years);

        foreach (var countryCode in countryCodes)
        {
            var merged = next.Holidays.TryGetValue(countryCode, out var seeded)
                ? new SortedSet<string>(seeded, StringComparer.OrdinalIgnoreCase)
                : new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var year in years)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var date in await FetchCountryYearAsync(countryCode, year, cancellationToken).ConfigureAwait(false))
                {
                    merged.Add(date);
                }
            }

            next.Holidays[countryCode] = merged;
        }

        _cache = next;
        SaveCache(next);
    }

    public void Dispose()
    {
        // The CoreHost network broker is host-owned. No widget-owned unmanaged resources are held here.
    }

    private async Task<IReadOnlyList<string>> FetchCountryYearAsync(
        string countryCode,
        int year,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _network!.ReadAsync(
                new WindowsWidgetNetworkReadRequest(
                    new Uri($"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}"),
                    new Dictionary<string, string>
                    {
                        ["Accept"] = "application/json",
                        ["Cache-Control"] = "no-cache"
                    }),
                cts.Token).ConfigureAwait(false);

            if (response.StatusCode < 200 || response.StatusCode >= 300 || response.Body.Length == 0)
            {
                return Array.Empty<string>();
            }

            var text = Encoding.UTF8.GetString(response.Body);
            using var json = JsonDocument.Parse(text);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var dates = new List<string>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("date", out var dateElement)
                    && dateElement.ValueKind == JsonValueKind.String
                    && DateOnly.TryParseExact(
                        dateElement.GetString(),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    dates.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
            }

            return dates;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static HolidayCache LoadCache()
    {
        try
        {
            if (!File.Exists(CachePath))
            {
                return SeededHolidayFallback.CreateCache(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1);
            }

            var json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<HolidayCache>(json) ?? SeededHolidayFallback.CreateCache(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1);
        }
        catch
        {
            return SeededHolidayFallback.CreateCache(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1);
        }
    }

    private static void SaveCache(HolidayCache cache)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            var tmp = CachePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, CachePath, overwrite: true);
        }
        catch
        {
            // Cache write failure is not a UI failure.
        }
    }
}

internal sealed class HolidayCache
{
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Dictionary<string, SortedSet<string>> Holidays { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class SeededHolidayFallback
{
    private static readonly Dictionary<string, string[]> FixedMonthDayByCountry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PT"] = ["01-01", "04-25", "05-01", "06-10", "12-25"],
        ["TR"] = ["01-01", "04-23", "05-01", "05-19", "08-30", "10-29"],
        ["HK"] = ["01-01", "05-01", "07-01", "10-01", "12-25"],
        ["CA"] = ["01-01", "07-01", "12-25"],
        ["US"] = ["01-01", "07-04", "12-25"],
        ["AE"] = ["01-01", "12-02"],
        ["VN"] = ["01-01", "04-30", "05-01", "09-02"],
        ["TH"] = ["01-01", "04-13", "05-01", "12-05"],
        ["SG"] = ["01-01", "05-01", "08-09", "12-25"],
        ["CN"] = ["01-01", "05-01", "10-01"],
        ["JP"] = ["01-01", "02-11", "05-03", "11-03"],
        ["KR"] = ["01-01", "03-01", "10-03"],
        ["TW"] = ["01-01", "02-28", "10-10"],
        ["ID"] = ["01-01", "08-17"],
        ["MY"] = ["01-01", "08-31"],
        ["PH"] = ["01-01", "06-12", "12-25"],
        ["IN"] = ["01-26", "08-15", "10-02"],
        ["AU"] = ["01-01", "01-26", "12-25"],
        ["GB"] = ["01-01", "12-25"],
        ["FR"] = ["01-01", "05-01", "07-14", "12-25"],
        ["DE"] = ["01-01", "05-01", "10-03", "12-25"],
        ["ES"] = ["01-01", "05-01", "10-12", "12-25"],
        ["IT"] = ["01-01", "04-25", "05-01", "06-02", "12-25"],
        ["NL"] = ["01-01", "04-27", "12-25"],
        ["CH"] = ["01-01", "08-01", "12-25"],
        ["MX"] = ["01-01", "09-16", "12-25"],
        ["BR"] = ["01-01", "04-21", "09-07", "12-25"],
        ["AR"] = ["01-01", "05-01", "07-09", "12-25"],
        ["CL"] = ["01-01", "05-01", "09-18", "12-25"],
        ["EG"] = ["01-07", "07-23", "10-06"],
        ["ZA"] = ["01-01", "04-27", "12-16", "12-25"],
        ["SA"] = ["02-22", "09-23"],
        ["QA"] = ["12-18"],
        ["IL"] = ["01-01"],
        ["GR"] = ["01-01", "03-25", "10-28", "12-25"],
        ["RU"] = ["01-01", "05-01", "05-09", "06-12"],
        ["PL"] = ["01-01", "05-01", "05-03", "11-11", "12-25"]
    };

    public static bool IsHoliday(string countryCode, string yyyyMmDd)
    {
        if (!DateOnly.TryParseExact(yyyyMmDd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        return FixedMonthDayByCountry.TryGetValue(countryCode, out var dates) && dates.Contains(date.ToString("MM-dd", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
    }

    public static HolidayCache CreateCache(params int[] years)
    {
        var cache = new HolidayCache { UpdatedAtUtc = DateTimeOffset.UtcNow };
        foreach (var (countryCode, dates) in AllDates(years))
        {
            cache.Holidays[countryCode] = new SortedSet<string>(dates, StringComparer.OrdinalIgnoreCase);
        }
        return cache;
    }

    public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> AllDates(params int[] years)
    {
        foreach (var (countryCode, monthDays) in FixedMonthDayByCountry)
        {
            var dates = years.SelectMany(year => monthDays.Select(md => $"{year:0000}-{md}"));
            yield return new KeyValuePair<string, IEnumerable<string>>(countryCode, dates);
        }
    }
}

internal enum WidgetDisplayMode
{
    Map,
    List
}

internal sealed record MapCityLayout(
    string Code,
    double Latitude,
    double Longitude,
    double Dx,
    double Dy,
    bool LabelLeft,
    bool SmallLabel = false);

internal enum CityDayStatus
{
    RestDay,
    WorkingHours,
    WorkdayOffHours,
    Holiday
}

internal sealed record CityTimeInfo(string HourMinute, string WeekdayDate, string ZoneAbbreviation, CityDayStatus Status);

internal sealed record CityDefinition(
    string Id,
    string City,
    string IanaZone,
    string WindowsZone,
    string StandardAbbreviation,
    string DaylightAbbreviation,
    string CountryCode,
    bool Protected = false)
{
    public TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return ResolveFallback();
        }
        catch (InvalidTimeZoneException)
        {
            return ResolveFallback();
        }
    }

    public string ZoneAbbreviation(DateTime localDateTime, TimeZoneInfo zone)
    {
        return zone.IsDaylightSavingTime(localDateTime) ? DaylightAbbreviation : StandardAbbreviation;
    }

    private TimeZoneInfo ResolveFallback()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
