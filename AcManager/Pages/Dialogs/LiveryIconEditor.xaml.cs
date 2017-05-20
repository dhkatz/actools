﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AcManager.CustomShowroom;
using AcManager.Tools;
using AcManager.Tools.Helpers;
using AcManager.Tools.Managers;
using AcManager.Tools.Objects;
using AcTools.Kn5File;
using AcTools.Render.Base.Utils;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Converters;
using JetBrains.Annotations;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Image = System.Drawing.Image;
using Size = System.Windows.Size;

namespace AcManager.Pages.Dialogs {
    public partial class LiveryIconEditor : INotifyPropertyChanged {
        public const string KeyStyle = "__LiveryIconEditor.style";
        public const string KeyShape = "__LiveryIconEditor.shape";
        public const string KeyNumbers = "__LiveryIconEditor.numbers";

        public SettingEntry[] Numbers { get; } = {
            new SettingEntry("", ToolsStrings.Common_None),
            new SettingEntry("Condensed", AppStrings.LiveryIcon_Number_Condensed),
            new SettingEntry("Light", AppStrings.LiveryIcon_Number_Light),
            new SettingEntry("LightOutlined", AppStrings.LiveryIcon_Number_LightOutlined),
        };

        public SettingEntry[] Shapes { get; } = {
            new SettingEntry("Flat", AppStrings.LiveryIcon_Shape_Flat),
            new SettingEntry("Diagonal", AppStrings.LiveryIcon_Shape_Diagonal),
            new SettingEntry("CornerTriangle", "Corner Triangle"),
            new SettingEntry("Stripes", AppStrings.LiveryIcon_Shape_Stripes),
            new SettingEntry("StripesSide", AppStrings.LiveryIcon_Shape_SideStripes),
            new SettingEntry("DoubleStripes", AppStrings.LiveryIcon_Shape_DoubleStripes),
            new SettingEntry("HorizontalStripes", AppStrings.LiveryIcon_Shape_HorizontalStripes),
            new SettingEntry("Circle", AppStrings.LiveryIcon_Shape_Circle),
            new SettingEntry("DiagonalWithCircle", AppStrings.LiveryIcon_Shape_DiagonalWithCircle),
            new SettingEntry("DiagonalLineWithCircle", AppStrings.LiveryIcon_Shape_DiagonalLineWithCircle),
            new SettingEntry("Carbon", AppStrings.LiveryIcon_Shape_Carbon),
            new SettingEntry("HorizontalSplit", AppStrings.LiveryIcon_Shape_HorizontalSplit),
            new SettingEntry("VerticalSplit", AppStrings.LiveryIcon_Shape_VerticalSplit),
            new SettingEntry("TripleHorizontalSplit", AppStrings.LiveryIcon_Shape_TripleHorizontalSplit),
            new SettingEntry("TripleVerticalSplit", AppStrings.LiveryIcon_Shape_TripleVerticalSplit),
        };

        public SettingEntry[] Styles { get; } = {
            new SettingEntry("Solid", AppStrings.LiveryIcon_Style_Solid),
            new SettingEntry("Gradient", AppStrings.LiveryIcon_Style_Gradient),
            new SettingEntry("Gloss", AppStrings.LiveryIcon_Style_Gloss),
            new SettingEntry("Bright", AppStrings.LiveryIcon_Style_Bright),
            new SettingEntry("Miura", AppStrings.LiveryIcon_Style_Miura),
            new SettingEntry("Tesla", AppStrings.LiveryIcon_Style_Tesla),
        };

        public CarSkinObject Skin { get; set; }

        public bool HasSecondaryColor => (CustomShape ? StyleColorsNumber : ShapeColorsNumber) > 1;

        public bool HasTertiaryColor => (CustomShape ? StyleColorsNumber : ShapeColorsNumber) > 2;

        private int _shapeColorsNumber;

        public int ShapeColorsNumber {
            get { return _shapeColorsNumber; }
            set {
                value = value.Clamp(1, 3);
                if (Equals(value, _shapeColorsNumber)) return;
                _shapeColorsNumber = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSecondaryColor));
                OnPropertyChanged(nameof(HasTertiaryColor));
            }
        }

        private int _styleColorsNumber;

        public int StyleColorsNumber {
            get { return _styleColorsNumber; }
            set {
                if (Equals(value, _styleColorsNumber)) return;
                _styleColorsNumber = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSecondaryColor));
                OnPropertyChanged(nameof(HasTertiaryColor));
            }
        }

        private bool _customShape;

        public bool CustomShape {
            get { return _customShape; }
            set {
                if (Equals(value, _customShape)) return;
                _customShape = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSecondaryColor));
                OnPropertyChanged(nameof(HasTertiaryColor));
            }
        }

        private void LoadProperties(FrameworkElement element, bool styleMode) {
            if (styleMode) {
                CustomShape = false;
                StyleColorsNumber = 3;
            } else {
                ShapeColorsNumber = 3;
            }

            var properties = element.Tag as string;
            if (properties == null) return;

            foreach (var s in properties.Split(';')) {
                var pair = s.Split(new[] { '=', ':' }, 2);
                if (pair.Length != 2) continue;

                var key = pair[0].ToLowerInvariant();
                if (styleMode) {
                    switch (key) {
                        case "customshape":
                            CustomShape = string.Equals(pair[1], @"true", StringComparison.OrdinalIgnoreCase);
                            break;
                    }
                }

                switch (key) {
                    case "colors":
                        if (styleMode) {
                            StyleColorsNumber = FlexibleParser.ParseInt(pair[1], 3);
                        } else {
                            ShapeColorsNumber = FlexibleParser.ParseInt(pair[1], 3);
                        }
                        break;
                }
            }
        }

        private SettingEntry _selectedShape;

        public SettingEntry SelectedShape {
            get { return _selectedShape; }
            set {
                if (Equals(value, _selectedShape)) return;
                _selectedShape = value;
                OnPropertyChanged();

                try {
                    var obj = (FrameworkElement)Application.LoadComponent(new Uri($"/Assets/Livery/Shapes/{value.Id}.xaml", UriKind.Relative));
                    LoadProperties(obj, false);
                    obj.DataContext = Model;
                    Model.ShapeObject = obj;
                    if (!_quickMode) ValuesStorage.Set(KeyShape, value.Id);
                } catch (Exception e) {
                    Logging.Warning("Can’t change shape: " + e);
                }
            }
        }

        private SettingEntry _selectedNumbers;

        public SettingEntry SelectedNumbers {
            get { return _selectedNumbers; }
            set {
                if (Equals(value, _selectedNumbers)) return;
                _selectedNumbers = value;
                OnPropertyChanged();

                try {
                    if (value.Id != string.Empty) {
                        var obj = (FrameworkElement)Application.LoadComponent(new Uri($"/Assets/Livery/Numbers/{value.Id}.xaml", UriKind.Relative));
                        obj.DataContext = Model;
                        Model.NumbersObject = obj;
                        if (!_quickMode) ValuesStorage.Set(KeyNumbers, value.Id);
                    } else {
                        Model.NumbersObject = null;
                    }
                } catch (Exception e) {
                    Logging.Warning("Can’t change numbers: " + e);
                }
            }
        }

        private SettingEntry _selectedStyle;

        public SettingEntry SelectedStyle {
            get { return _selectedStyle; }
            set {
                if (Equals(value, _selectedStyle)) return;
                _selectedStyle = value;
                OnPropertyChanged();

                try {
                    StyleObject = (FrameworkElement)Application.LoadComponent(new Uri($"/Assets/Livery/Styles/{value.Id}.xaml", UriKind.Relative));
                    LoadProperties(StyleObject, true);
                    StyleObject.DataContext = Model;
                    OnPropertyChanged(nameof(StyleObject));
                    if (!_quickMode) ValuesStorage.Set(KeyStyle, value.Id);
                } catch (Exception e) {
                    Logging.Warning("Can’t change style: " + e);
                }
            }
        }

        public FrameworkElement StyleObject { get; private set; }

        public LiveryIconEditor(CarSkinObject skin) : this(skin, false, false, null, null) { }

        private readonly bool _quickMode;

        private LiveryIconEditor(CarSkinObject skin, bool quickMode, bool randomMode, [CanBeNull] Color[] colors, [CanBeNull] string preferredStyle) {
            _quickMode = quickMode;
            Skin = skin;

            DataContext = this;
            InitializeComponent();

            if (preferredStyle != null) {
                SelectedShape = Shapes.GetByIdOrDefault(preferredStyle) ?? Shapes.FirstOrDefault(x => x.DisplayName == preferredStyle) ??
                        Shapes.RandomElement();
            } else if (randomMode) {
                SelectedShape = Shapes.RandomElement();
            } else {
                SelectedShape = Shapes.GetByIdOrDefault(ValuesStorage.GetString(KeyShape)) ?? Shapes.FirstOrDefault();
            }

            SelectedStyle = Styles.GetByIdOrDefault(ValuesStorage.GetString(KeyStyle)) ?? Styles.FirstOrDefault();
            SelectedNumbers = string.IsNullOrWhiteSpace(skin.SkinNumber) || skin.SkinNumber == @"0"
                    ? Numbers.FirstOrDefault() : Numbers.GetByIdOrDefault(ValuesStorage.GetString(KeyNumbers)) ?? Numbers.FirstOrDefault();

            Buttons = new[] { OkButton, CancelButton };
            Model.Value = string.IsNullOrWhiteSpace(skin.SkinNumber) ? @"0" : skin.SkinNumber;
            Model.TextColorValue = Colors.White;

            colors = colors ?? GuessColors(skin);
            Model.ColorValue = colors.Length > 0 ? colors[0] : Colors.White;
            Model.SecondaryColorValue = colors.Length > 1 ? colors[1] : Colors.Black;
            Model.TertiaryColorValue = colors.Length > 2 ? colors[2] : Colors.Black;
        }

        private static readonly WeakList<Tuple<string, Kn5>> Kn5MaterialsCache = new WeakList<Tuple<string, Kn5>>(10);
        private static readonly WeakList<Tuple<string, Kn5>> Kn5TexturesCache = new WeakList<Tuple<string, Kn5>>(10);

        [CanBeNull]
        private static Color[] GuessColorsFromTextures(CarSkinObject skin) {
            if (!ImageUtils.IsMagickSupported) {
                Logging.Debug("ImageMagick is missing");
                return null;
            }

            var car = CarsManager.Instance.GetById(skin.CarId);
            if (car == null) {
                Logging.Debug("Car not found");
                return null;
            }

            Kn5MaterialsCache.Purge();
            Kn5TexturesCache.Purge();

            string kn5Filename = null;
            Func<string> getKn5Filename = () => kn5Filename ??
                    (kn5Filename = FileUtils.GetMainCarFilename(car.Location, car.AcdData));

            var materialsKn5 = Kn5MaterialsCache.FirstOrDefault(x => x?.Item1 == car.Id)?.Item2;
            if (materialsKn5 == null) {
                if (!File.Exists(getKn5Filename())) {
                    Logging.Debug("KN5 not found");
                    return null;
                }

                materialsKn5 = Kn5.FromFile(getKn5Filename(), SkippingTextureLoader.Instance, nodeLoader: SkippingNodeLoader.Instance);
                Kn5MaterialsCache.Add(Tuple.Create(car.Id, materialsKn5));
            }

            var paintable = PaintShop.GetPaintableItems(skin.CarId, materialsKn5).ToList();

            var carPaint = paintable.OfType<PaintShop.CarPaint>().FirstOrDefault();
            if (carPaint?.GuessColorsFromPreviews != false) {
                return null;
            }

            var texture = carPaint.DetailsTexture;
            if (texture == null) {
                Logging.Debug("Details texture not found");
                return null;
            }

            if (!File.Exists(Path.Combine(skin.Location, texture))) {
                Logging.Debug("Details texture not overridden, could be a fully custom skin");
                return null;
            }

            TextureReader reader = null;
            var s = Stopwatch.StartNew();
            try {
                Kn5 texturesKn5 = null;
                Func<string, Color> getColor = textureName => {
                    if (textureName == null) return Colors.White;

                    var filename = Path.Combine(skin.Location, textureName);
                    if (!File.Exists(filename)) {
                        if (texturesKn5 == null) {
                            texturesKn5 = Kn5TexturesCache.FirstOrDefault(x => x?.Item1 == car.Id)?.Item2;
                            if (texturesKn5 == null) {
                                if (!File.Exists(getKn5Filename())) return Colors.White;
                                texturesKn5 = Kn5.FromFile(kn5Filename, materialLoader: SkippingMaterialLoader.Instance, nodeLoader: SkippingNodeLoader.Instance);
                                Kn5TexturesCache.Add(Tuple.Create(car.Id, texturesKn5));
                            }
                        }

                        var bytes = texturesKn5.TexturesData.GetValueOrDefault(textureName);
                        if (bytes == null) return Colors.White;

                        if (reader == null) {
                            reader = new TextureReader();
                        }

                        return ImageUtils.GetTextureColor(
                                reader.ToPngNoFormat(bytes, true, new System.Drawing.Size(32, 32))).ToColor();
                    }

                    if (reader == null) {
                        reader = new TextureReader();
                    }

                    return ImageUtils.GetTextureColor(
                            reader.ToPngNoFormat(File.ReadAllBytes(filename), true, new System.Drawing.Size(32, 32))).ToColor();
                };

                var result = new[] {
                    getColor(texture),
                    Colors.Black,
                    Colors.Black
                };

                Logging.Debug($"Main color: {texture} ({result[0].ToHexString()})");

                foreach (var item in paintable.OfType<PaintShop.ColoredItem>().Where(x => x.LiveryColorIds?.Length > 0).OrderBy(x => x.LiveryPriority)) {
                    if (item.LiveryColorIds == null) continue;
                    for (var i = 0; i < item.LiveryColorIds.Length; i++) {
                        var slotId = i;
                        var slotTexture = item.AffectedTextures.ElementAtOrDefault(item.LiveryColorIds[i]);
                        Logging.Debug($"Extra: {slotId} = {slotTexture} (priority: {item.LiveryPriority})");

                        if (slotId < 0 || slotId > 2 || slotTexture == null) continue;
                        result[slotId] = getColor(slotTexture);
                    }
                }

                Logging.Debug($"Colors guessed: {s.Elapsed.TotalMilliseconds:F1} ms");
                return result;
            } finally {
                DisposeHelper.Dispose(ref reader);
            }
        }

        [NotNull]
        private static Color[] GuessColors(CarSkinObject skin) {
            try {
                var result = GuessColorsFromTextures(skin);
                if (result != null) {
                    return result;
                }
            } catch (Exception e) {
                Logging.Warning("Can’t guess colors with Paint Shop: " + e);
            }

            try {
                using (var bitmap = Image.FromFile(skin.PreviewImage)) {
                    var baseColors = ImageUtils.GetBaseColors((Bitmap)bitmap);
                    var a = baseColors.Select(x => (System.Drawing.Color?)x).FirstOrDefault()?.ToColor() ?? Colors.White;
                    var b = baseColors.Select(x => (System.Drawing.Color?)x).ElementAtOrDefault(1)?.ToColor() ?? Colors.Black;
                    var c = baseColors.Select(x => (System.Drawing.Color?)x).ElementAtOrDefault(2)?.ToColor() ?? Colors.Black;
                    return new[] { a, b, c };
                }
            } catch (Exception e) {
                Logging.Warning("Can’t guess colors: " + e);
                return new[] { Colors.White, Colors.Black, Colors.Black };
            }
        }

        public new bool ShowDialog() {
            base.ShowDialog();
            return IsResultOk;
        }

        public StyleViewModel Model { get; } = new StyleViewModel();

        public class StyleViewModel : NotifyPropertyChanged {
            public Grid BaseObject { get; }

            public VisualBrush Base { get; }

            internal StyleViewModel() {
                BaseObject = new Grid {
                    Width = 64,
                    Height = 64
                };
                Base = new VisualBrush(BaseObject);
            }

            private FrameworkElement _shapeObject;

            [NotNull]
            public FrameworkElement ShapeObject {
                get { return _shapeObject; }
                set {
                    if (Equals(value, _shapeObject)) return;
                    _shapeObject = value;
                    _shapeObject.Width = 64;
                    _shapeObject.Height = 64;
                    OnPropertyChanged();

                    BaseObject.Children.Clear();
                    BaseObject.Children.Add(_shapeObject);
                    if (_numbersObject != null) {
                        BaseObject.Children.Add(_numbersObject);
                    }

                    Shape = new VisualBrush(value) { Stretch = Stretch.None };
                    OnPropertyChanged(nameof(Shape));
                }
            }

            private FrameworkElement _numbersObject;

            [CanBeNull]
            public FrameworkElement NumbersObject {
                get { return _numbersObject; }
                set {
                    if (Equals(value, _numbersObject)) return;
                    _numbersObject = value;
                    OnPropertyChanged();

                    BaseObject.Children.Clear();
                    BaseObject.Children.Add(_shapeObject);

                    if (_numbersObject != null) {
                        _numbersObject.Width = 64;
                        _numbersObject.Height = 64;
                        BaseObject.Children.Add(_numbersObject);

                        Number = new VisualBrush(value) { Stretch = Stretch.None };
                    } else {
                        Number = new SolidColorBrush(Colors.Transparent);
                    }

                    OnPropertyChanged(nameof(Number));
                }
            }

            public Brush Shape { get; private set; }

            public Brush Number { get; private set; }

            private Color? _colorValue;

            public Color ColorValue {
                get { return _colorValue ?? Colors.Black; }
                set {
                    if (Equals(value, _colorValue)) return;
                    _colorValue = value;
                    OnPropertyChanged();

                    Color = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(Color));
                }
            }

            public SolidColorBrush Color { get; private set; }

            private Color? _secondaryColorValue;

            public Color SecondaryColorValue {
                get { return _secondaryColorValue ?? Colors.Black; }
                set {
                    if (Equals(value, _secondaryColorValue)) return;
                    _secondaryColorValue = value;
                    OnPropertyChanged();

                    SecondaryColor = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(SecondaryColor));
                }
            }

            public SolidColorBrush SecondaryColor { get; private set; }

            private Color? _tertiaryColorValue;

            public Color TertiaryColorValue {
                get { return _tertiaryColorValue ?? Colors.Black; }
                set {
                    if (Equals(value, _tertiaryColorValue)) return;
                    _tertiaryColorValue = value;
                    OnPropertyChanged();

                    TertiaryColor = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(TertiaryColor));
                }
            }

            public SolidColorBrush TertiaryColor { get; private set; }

            private string _value;

            public string Value {
                get { return _value; }
                set {
                    if (Equals(value, _value)) return;
                    _value = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NumberValue));
                }
            }

            public int NumberValue {
                get { return _value.AsInt(); }
                set { Value = value.ToInvariantString(); }
            }

            private Color? _textColorValue;

            public Color TextColorValue {
                get { return _textColorValue ?? Colors.Black; }
                set {
                    if (Equals(value, _textColorValue)) return;
                    _textColorValue = value;
                    OnPropertyChanged();

                    TextColor = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(TextColor));

                    BaseObject.SetValue(TextBlock.ForegroundProperty, TextColor);
                }
            }

            public SolidColorBrush TextColor { get; private set; }
        }

        private static readonly Action EmptyDelegate = delegate { };

        private void OnClosing(object sender, CancelEventArgs e) {
            if (IsResultOk) {
                CreateNewIcon().Forget();
            }
        }

        private async Task CreateNewIcon() {
            var size = new Size(64, 64);
            Result.Measure(size);
            Result.Arrange(new Rect(size));
            Result.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            await Task.Delay(100);
            if (Skin == null) return;

            // TODO: save style?
            var bmp = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(Result);

            if (Model.Value != @"0" && Model.Value != Skin.SkinNumber &&
                    ShowMessage(string.Format(AppStrings.LiveryIcon_ChangeNumber_Message, Model.Value), AppStrings.LiveryIcon_ChangeNumber_Title,
                            MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                Skin.SkinNumber = Model.Value;
            }

            try {
                bmp.SaveAsPng(Skin.LiveryImage);
            } catch (IOException e) {
                NonfatalError.Notify(AppStrings.LiveryIcon_CannotChange, AppStrings.LiveryIcon_CannotChange_Commentary, e);
            } catch (Exception e) {
                NonfatalError.Notify(AppStrings.LiveryIcon_CannotChange, e);
            }
        }
        
        public static Task GenerateAsync(CarSkinObject target, Color[] colors, string preferredStyle) {
            return new LiveryIconEditor(target, true, false, colors, preferredStyle).CreateNewIcon();
        }

        public static Task GenerateAsync(CarSkinObject target) {
            return new LiveryIconEditor(target, true, false, null, null).CreateNewIcon();
        }

        public static Task GenerateRandomAsync(CarSkinObject target) {
            return new LiveryIconEditor(target, true, true, null, null).CreateNewIcon();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
