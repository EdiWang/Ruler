using System.Resources;

namespace Ruler;

public sealed class MainForm : Form
{
    #region ResizeRegion enum

    private enum ResizeRegion
    {
        None, N, NE, E, SE, S, SW, W, NW
    }

    #endregion

    #region Constants

    private const int DefaultResizeBorderWidth = 5;
    private const int SmallMovement = 1;
    private const int LargeMovement = 5;
    private const double DefaultOpacity = 0.65;
    private const int DefaultWidth = 512;
    private const int DefaultHeight = 128;

    #endregion

    #region Fields

    private readonly ToolTip _toolTip = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly Font _rulerFont = new("Segoe UI", 10);

    private Point _offset;
    private Rectangle _mouseDownRect;
    private readonly int _resizeBorderWidth = DefaultResizeBorderWidth;
    private Point _mouseDownPoint;
    private ResizeRegion _resizeRegion = ResizeRegion.None;

    private ToolStripMenuItem _verticalMenuItem;
    private ToolStripMenuItem _toolTipMenuItem;

    #endregion

    #region Constructor

    public MainForm()
    {
        InitializeComponent();
        SetUpMenu();
    }

    #endregion

    #region Properties

    private bool IsVertical
    {
        get => _verticalMenuItem?.Checked ?? false;
        set
        {
            if (_verticalMenuItem != null)
                _verticalMenuItem.Checked = value;
        }
    }

    private bool ShowToolTip
    {
        get => _toolTipMenuItem?.Checked ?? false;
        set
        {
            if (_toolTipMenuItem != null)
            {
                _toolTipMenuItem.Checked = value;
                if (value)
                {
                    SetToolTip();
                }
            }
        }
    }

    #endregion

    #region Initialization

    private void InitializeComponent()
    {
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        UpdateStyles();

        LoadIcon();
        ConfigureAppearance();
    }

    private void LoadIcon()
    {
        try
        {
            var resources = new ResourceManager(typeof(MainForm));
            if (resources.GetObject("$this.Icon") is Icon icon)
            {
                Icon = icon;
            }
        }
        catch
        {
            // Fallback to default icon if resource loading fails
        }
    }

    private void ConfigureAppearance()
    {
        Text = "Ruler";
        BackColor = Color.White;
        ClientSize = new Size(DefaultWidth, DefaultHeight);
        FormBorderStyle = FormBorderStyle.None;
        Opacity = DefaultOpacity;
        ContextMenuStrip = _menu;
        Font = _rulerFont;
    }

    private void SetUpMenu()
    {
        AddMenuItem("Stay On Top");
        _verticalMenuItem = AddMenuItem("Vertical");
        _toolTipMenuItem = AddMenuItem("Tool Tip");
        var opacityMenuItem = AddMenuItem("Opacity");
        _menu.Items.Add(new ToolStripSeparator());
        AddMenuItem("About");
        _menu.Items.Add(new ToolStripSeparator());
        AddMenuItem("Exit");

        CreateOpacitySubMenu(opacityMenuItem);
    }

    private void CreateOpacitySubMenu(ToolStripMenuItem opacityMenuItem)
    {
        for (int i = 10; i <= 100; i += 10)
        {
            var subMenu = new ToolStripMenuItem($"{i}%");
            subMenu.Click += OpacityMenuHandler;
            opacityMenuItem.DropDownItems.Add(subMenu);
        }
    }

    private ToolStripMenuItem AddMenuItem(string text, Keys shortcut = Keys.None)
    {
        var menuItem = new ToolStripMenuItem(text)
        {
            ShortcutKeys = shortcut
        };
        menuItem.Click += MenuHandler;
        _menu.Items.Add(menuItem);
        return menuItem;
    }

    #endregion

    #region Mouse Handling

    protected override void OnMouseDown(MouseEventArgs e)
    {
        var mousePos = MousePosition;
        _offset = new Point(mousePos.X - Location.X, mousePos.Y - Location.Y);
        _mouseDownPoint = mousePos;
        _mouseDownRect = ClientRectangle;

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _resizeRegion = ResizeRegion.None;
        base.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_resizeRegion != ResizeRegion.None)
        {
            HandleResize();
            return;
        }

        var clientCursorPos = PointToClient(MousePosition);
        var resizeInnerRect = ClientRectangle;
        resizeInnerRect.Inflate(-_resizeBorderWidth, -_resizeBorderWidth);

        bool inResizableArea = ClientRectangle.Contains(clientCursorPos) &&
                              !resizeInnerRect.Contains(clientCursorPos);

        if (inResizableArea)
        {
            var resizeRegion = GetResizeRegion(clientCursorPos);
            SetResizeCursor(resizeRegion);

            if (e.Button == MouseButtons.Left)
            {
                _resizeRegion = resizeRegion;
                HandleResize();
            }
        }
        else
        {
            Cursor = Cursors.Default;

            if (e.Button == MouseButtons.Left)
            {
                var mousePos = MousePosition;
                Location = new Point(mousePos.X - _offset.X, mousePos.Y - _offset.Y);
            }
        }

        base.OnMouseMove(e);
    }

    #endregion

    #region Keyboard Handling

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Right:
            case Keys.Left:
            case Keys.Up:
            case Keys.Down:
                HandleMoveResizeKeystroke(e);
                break;
            case Keys.Space:
                ChangeOrientation();
                break;
        }

        base.OnKeyDown(e);
    }

    private void HandleMoveResizeKeystroke(KeyEventArgs e)
    {
        var movement = e.Control ? SmallMovement : LargeMovement;

        switch (e.KeyCode)
        {
            case Keys.Right:
                if (e.Control && e.Shift)
                    Width += SmallMovement;
                else
                    Left += movement;
                break;

            case Keys.Left:
                if (e.Control && e.Shift)
                    Width -= SmallMovement;
                else
                    Left -= movement;
                break;

            case Keys.Up:
                if (e.Control && e.Shift)
                    Height -= SmallMovement;
                else
                    Top -= movement;
                break;

            case Keys.Down:
                if (e.Control && e.Shift)
                    Height += SmallMovement;
                else
                    Top += movement;
                break;
        }
    }

    #endregion

    #region Resize Handling

    private void HandleResize()
    {
        var mouseDiff = new Point(
            MousePosition.X - _mouseDownPoint.X,
            MousePosition.Y - _mouseDownPoint.Y);

        switch (_resizeRegion)
        {
            case ResizeRegion.E:
                Width = _mouseDownRect.Width + mouseDiff.X;
                break;
            case ResizeRegion.S:
                Height = _mouseDownRect.Height + mouseDiff.Y;
                break;
            case ResizeRegion.SE:
                Width = _mouseDownRect.Width + mouseDiff.X;
                Height = _mouseDownRect.Height + mouseDiff.Y;
                break;
        }
    }

    private void SetResizeCursor(ResizeRegion region)
    {
        Cursor = region switch
        {
            ResizeRegion.N or ResizeRegion.S => Cursors.SizeNS,
            ResizeRegion.E or ResizeRegion.W => Cursors.SizeWE,
            ResizeRegion.NW or ResizeRegion.SE => Cursors.SizeNWSE,
            _ => Cursors.SizeNESW
        };
    }

    private ResizeRegion GetResizeRegion(Point clientCursorPos)
    {
        bool isTop = clientCursorPos.Y <= _resizeBorderWidth;
        bool isBottom = clientCursorPos.Y >= Height - _resizeBorderWidth;
        bool isLeft = clientCursorPos.X <= _resizeBorderWidth;
        bool isRight = clientCursorPos.X >= Width - _resizeBorderWidth;

        return (isTop, isBottom, isLeft, isRight) switch
        {
            (true, false, true, false) => ResizeRegion.NW,
            (true, false, false, true) => ResizeRegion.NE,
            (true, false, false, false) => ResizeRegion.N,
            (false, true, true, false) => ResizeRegion.SW,
            (false, true, false, true) => ResizeRegion.SE,
            (false, true, false, false) => ResizeRegion.S,
            (false, false, true, false) => ResizeRegion.W,
            _ => ResizeRegion.E
        };
    }

    #endregion

    #region Drawing

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var (width, height) = PrepareGraphicsForOrientation(graphics);
        DrawRuler(graphics, width, height);
        base.OnPaint(e);
    }

    private (int width, int height) PrepareGraphicsForOrientation(Graphics graphics)
    {
        if (IsVertical)
        {
            graphics.RotateTransform(90);
            graphics.TranslateTransform(0, -Width + 1);
            return (Height, Width);
        }
        return (Width, Height);
    }

    private void DrawRuler(Graphics g, int formWidth, int formHeight)
    {
        DrawBorder(g, formWidth, formHeight);
        DrawDimensionLabel(g, formWidth, formHeight);
        DrawTicks(g, formWidth, formHeight);
    }

    private static void DrawBorder(Graphics g, int formWidth, int formHeight)
    {
        g.DrawRectangle(Pens.Black, 0, 0, formWidth - 1, formHeight - 1);
    }

    private void DrawDimensionLabel(Graphics g, int formWidth, int formHeight)
    {
        var text = $"{formWidth} pixels";
        var y = formHeight / 2 - Font.Height / 2;
        g.DrawString(text, Font, Brushes.Black, 10, y);
    }

    private void DrawTicks(Graphics g, int formWidth, int formHeight)
    {
        for (int i = 0; i < formWidth; i += 2)
        {
            var tickHeight = GetTickHeight(i);
            DrawTick(g, i, formHeight, tickHeight);

            if (i % 100 == 0)
            {
                DrawTickLabel(g, i.ToString(), i, formHeight, tickHeight);
            }
        }
    }

    private static int GetTickHeight(int position) => position switch
    {
        _ when position % 100 == 0 => 15,
        _ when position % 10 == 0 => 10,
        _ => 5
    };

    private static void DrawTick(Graphics g, int xPos, int formHeight, int tickHeight)
    {
        g.DrawLine(Pens.Black, xPos, 0, xPos, tickHeight);
        g.DrawLine(Pens.Black, xPos, formHeight, xPos, formHeight - tickHeight);
    }

    private void DrawTickLabel(Graphics g, string text, int xPos, int formHeight, int height)
    {
        g.DrawString(text, Font, Brushes.Black, xPos, height);
        g.DrawString(text, Font, Brushes.Black, xPos, formHeight - height - Font.Height);
    }

    #endregion

    #region Event Handlers

    protected override void OnResize(EventArgs e)
    {
        if (ShowToolTip)
        {
            SetToolTip();
        }
        base.OnResize(e);
    }

    private void SetToolTip()
    {
        _toolTip.SetToolTip(this, $"Width: {Width} pixels\nHeight: {Height} pixels");
    }

    private void OpacityMenuHandler(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Text: var text } &&
            double.TryParse(text.Replace("%", ""), out var value))
        {
            Opacity = value / 100;
        }
    }

    private void MenuHandler(object sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem) return;

        switch (menuItem.Text)
        {
            case "Exit":
                Close();
                break;
            case "Tool Tip":
                ShowToolTip = !ShowToolTip;
                break;
            case "Vertical":
                ChangeOrientation();
                break;
            case "Stay On Top":
                menuItem.Checked = !menuItem.Checked;
                TopMost = menuItem.Checked;
                break;
            case "About":
                ShowAboutDialog();
                break;
        }
    }

    private static void ShowAboutDialog()
    {
        var message = $"Ruler v{Application.ProductVersion} by Jeff Key, Edi Wang\n" +
                     "www.sliver.com, edi.wang\n" +
                     "Icon by Kristen Magee @ www.kbecca.com";
        MessageBox.Show(message, "About Ruler", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ChangeOrientation()
    {
        IsVertical = !IsVertical;
        (Width, Height) = (Height, Width);
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip?.Dispose();
            _menu?.Dispose();
            _rulerFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
