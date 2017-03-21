﻿using System;
using NControl.Abstractions;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NControl.Controls
{
	/// <summary>
	/// Tab strip control.
	/// </summary>
	public class TabStripControl: NControlView
	{
		#region Private Members

		/// <summary>
		/// The in transition.
		/// </summary>
		private bool _inTransition = false;

		/// <summary>
		/// The list of views that can be displayed.
		/// </summary>
		private readonly ObservableCollection<TabItem> _children = new ObservableCollection<TabItem> ();

		/// <summary>
		/// The tab control.
		/// </summary>
		private readonly NControlView _tabControl;

		/// <summary>
		/// The content view.
		/// </summary>
		private readonly Grid _contentView;

	    private ScrollView _scrollPage;

		/// <summary>
		/// The button stack.
		/// </summary>
		private readonly StackLayout _buttonStack;

		/// <summary>
		/// The indicator.
		/// </summary>
		private readonly TabBarIndicator _indicator;

		/// <summary>
		/// The layout.
		/// </summary>
		private readonly Grid _mainLayout;

		/// <summary>
		/// The shadow.
		/// </summary>
		private readonly NControlView _shadow;

	    private double _distanceX;
        private double _startX;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when tab activated.
        /// </summary>
        public event EventHandler<int> TabActivated;



		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="NControl.Controls.TabStripControl"/> class.
		/// </summary>
		public TabStripControl ()
		{
			_mainLayout = new Grid();
            _scrollPage = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = _mainLayout
            };
			Content = _scrollPage;

			// Create tab control
			_buttonStack = new StackLayoutEx {
				Orientation = StackOrientation.Horizontal,
				Padding = 0,
				Spacing = 0,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions= LayoutOptions.FillAndExpand,
			};

			_indicator = new TabBarIndicator {
				VerticalOptions = LayoutOptions.End,
				HorizontalOptions = LayoutOptions.Start,
				BackgroundColor = (Color)TabIndicatorColorProperty.DefaultValue,
				HeightRequest = 6,
				WidthRequest = 0,
			};

			_tabControl = new NControlView{
				BackgroundColor = TabBackColor,
				Content = new Grid{
					Padding = 0,
					ColumnSpacing = 0,
					RowSpacing=0,
					Children = {
						_buttonStack,
						_indicator,
					}
				}
			};

			_mainLayout.Children.AddHorizontal(_tabControl);

			// Create content control
			_contentView = new Grid {
				ColumnSpacing = 0,
				RowSpacing = 0,
				Padding = 0,
				BackgroundColor = Color.Transparent,
			};

			_mainLayout.Children.AddHorizontal(_contentView);

			_children.CollectionChanged += (sender, e) => {

				_contentView.Children.Clear();
				_buttonStack.Children.Clear();

				foreach(var tabChild in Children)
				{
					var tabItemControl = new TabBarButton(tabChild.Title);
					if(FontFamily != null)
						tabItemControl.FontFamily = FontFamily;

					tabItemControl.FontSize = FontSize;
					tabItemControl.SelectedColor = TabIndicatorColor;						
					_buttonStack.Children.Add(tabItemControl);
				}

				if(Children.Any())
					Activate(Children.First(), false);
			};

			// Border
			var border = new NControlView {
				DrawingFunction = (canvas, rect) => {

					canvas.DrawPath (new NGraphics.PathOp[]{
						new NGraphics.MoveTo(0, 0),
						new NGraphics.LineTo(rect.Width, 0)
					}, NGraphics.Pens.Gray, null);
				},
			};

			_mainLayout.Children.AddHorizontal(border);

			// Shadow
			_shadow = new NControlView {				
				DrawingFunction = (canvas, rect)=> {

                    canvas.DrawRectangle(rect, new NGraphics.Size(0, 0), null, new NGraphics.LinearGradientBrush(
						new NGraphics.Point(0.5, 0.0), new NGraphics.Point(0.5, 1.0),
						Color.Black.MultiplyAlpha(0.3).ToNColor(), NGraphics.Colors.Clear, 
						NGraphics.Colors.Clear));
				}
			};

			_mainLayout.Children.AddHorizontal(_shadow);

			_shadow.IsVisible = false;

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NControl.Controls.TabStripControl"/> class.
		/// </summary>
		/// <param name="view">View.</param>
		public void Activate (TabItem tabChild, bool animate)
		{
			var existingChild = Children.FirstOrDefault (t => t.View == 
				_contentView.Children.FirstOrDefault (v => v.IsVisible));

			if (existingChild == tabChild)
				return;

			var idxOfExisting = existingChild != null ? Children.IndexOf (existingChild) : -1;
			var idxOfNew = Children.IndexOf (tabChild);

			if (idxOfExisting > -1 && animate) 
			{
				_inTransition = true;

				// Animate
				var translation = idxOfExisting < idxOfNew ? 
					_contentView.Width : - _contentView.Width;

				tabChild.View.TranslationX = translation;
				if (tabChild.View.Parent != _contentView)
					_contentView.Children.Add(tabChild.View);
				else
					tabChild.View.IsVisible = true;

				var newElementWidth = _buttonStack.Children.ElementAt (idxOfNew).Width;
				var newElementLeft = _buttonStack.Children.ElementAt (idxOfNew).X;

				var animation = new Animation ();
				var existingViewOutAnimation = new Animation ((d) => existingChild.View.TranslationX = d,
					0, -translation, Easing.CubicInOut, () => {
						existingChild.View.IsVisible = false;
						_inTransition = false;
					});

				var newViewInAnimation = new Animation ((d) => tabChild.View.TranslationX = d,
					translation, 0, Easing.CubicInOut);

				var existingTranslation = _indicator.TranslationX;

				var indicatorTranslation = newElementLeft;
				var indicatorViewAnimation = new Animation ((d) => _indicator.TranslationX = d,
					existingTranslation, indicatorTranslation, Easing.CubicInOut);

				var startWidth = _indicator.Width;
				var indicatorSizeAnimation = new Animation ((d) => _indicator.WidthRequest = d,
					startWidth, newElementWidth, Easing.CubicInOut);

				animation.Add (0.0, 1.0, existingViewOutAnimation);
				animation.Add (0.0, 1.0, newViewInAnimation);
				animation.Add (0.0, 1.0, indicatorViewAnimation);
				animation.Add (0.0, 1.0, indicatorSizeAnimation);
				animation.Commit (this, "TabAnimation");
			} 
			else 
			{
				// Just set first view
				_contentView.Children.Clear();
				_contentView.Children.Add(tabChild.View);
			}

			foreach (var tabBtn in _buttonStack.Children)
				((TabBarButton)tabBtn).IsSelected = _buttonStack.Children.IndexOf(tabBtn) == idxOfNew;

			if (TabActivated != null)
				TabActivated(this, idxOfNew);
		}

	    public override bool TouchesMoved (IEnumerable<NGraphics.Point> points)
	    {
	        base.TouchesMoved(points);
            return HandleTouches(points, false);
        }

        /// <summary>
        /// Toucheses the began.
        /// </summary>
        /// <param name="points">Points.</param>
        public override bool TouchesBegan(IEnumerable<NGraphics.Point> points)
        {
            base.TouchesBegan(points);
            _startX = points.First().X;
            _distanceX = _startX;            
            return HandleTouches(points, false);
        }

        /// <summary>
        /// Toucheses the ended.
        /// </summary>
        /// <returns><c>true</c>, if ended was touchesed, <c>false</c> otherwise.</returns>
        /// <param name="points">Points.</param>
        public override bool TouchesEnded (IEnumerable<NGraphics.Point> points)
		{
			base.TouchesEnded (points);
            _distanceX -= points.First().X;
			return HandleTouches(points);
		}

		/// <summary>
		/// Toucheses the cancelled.
		/// </summary>
		/// <returns><c>true</c>, if cancelled was touchesed, <c>false</c> otherwise.</returns>
		/// <param name="points">Points.</param>
		public override bool TouchesCancelled (IEnumerable<NGraphics.Point> points)
		{
			base.TouchesCancelled (points);
			return HandleTouches(points);
		}

		/// <summary>
		/// Handles the touches.
		/// </summary>
		/// <param name="points">Points.</param>
		private bool HandleTouches(IEnumerable<NGraphics.Point> points, bool activate = true)
		{
			// Find selected item based on click
			var p = points.First();
			foreach (var child in _buttonStack.Children) 
			{
				if (p.X >= child.X && p.X <= child.X + child.Width && 
					p.Y >= child.Y && p.Y <= child.Y + _tabControl.Height) {

					if (activate && _startX != _distanceX)
					{
						var idx = _buttonStack.Children.IndexOf(child);
						Activate(Children[idx], true);
					}

					    if (_startX!=_distanceX)
					    {
					        _scrollPage.ScrollToAsync(_startX+_distanceX,0,true);
					    }

					    return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Positions and sizes the children of a Layout.
		/// </summary>
		/// <remarks>Implementors wishing to change the default behavior of a Layout should override this method. It is suggested to
		/// still call the base method and modify its calculated results.</remarks>
		protected override void LayoutChildren (double x, double y, double width, double height)
		{
			base.LayoutChildren (x, y, width, height);

			//if (width > 0 && !_inTransition) 
			//{
			//	var existingChild = Children.FirstOrDefault (t => 
			//		t.View == _contentView.Children.FirstOrDefault (v => v.IsVisible));

			//	var idxOfExisting = existingChild != null ? Children.IndexOf (existingChild) : -1;

			//	_indicator.WidthRequest = _buttonStack.Children.ElementAt(idxOfExisting).Width;
			//	_indicator.TranslationX = _buttonStack.Children.ElementAt(idxOfExisting).X;
			//}
		}

		/// <summary>
		/// Gets the views.
		/// </summary>
		/// <value>The views.</value>
		public IList<TabItem> Children
		{
			get{ return _children;}
		}

		/// <summary>
		/// The FontSize property.
		/// </summary>
		public static BindableProperty FontSizeProperty =
			BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TabStripControl), 14.0,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.FontSize = (double)newValue;
				});

		/// <summary>
		/// Gets or sets the FontSize of the TabBarButton instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public double FontSize
		{
			get { return (double)GetValue(FontSizeProperty); }
			set
			{
				SetValue(FontSizeProperty, value);
				foreach (var tabBtn in _buttonStack.Children)
					((TabBarButton)tabBtn).FontSize = value;
			}
		}

		/// <summary>
		/// The FontFamily property.
		/// </summary>
		public static BindableProperty FontFamilyProperty = 
			BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(TabStripControl), null,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.FontFamily = (string)newValue;
				});

		/// <summary>
		/// Gets or sets the FontFamily of the TabStripControl instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public string FontFamily {
			get{ return (string)GetValue (FontFamilyProperty); }
			set {
				SetValue (FontFamilyProperty, value);
				foreach (var tabBtn in _buttonStack.Children)
					((TabBarButton)tabBtn).FontFamily = value;
			}
		}

		/// <summary>
		/// The TabIndicatorColor property.
		/// </summary>
		public static BindableProperty TabIndicatorColorProperty = 
			BindableProperty.Create(nameof(TabIndicatorColor), typeof(Color), typeof(TabStripControl), Color.Accent,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.TabIndicatorColor = (Color)newValue;
				});

		/// <summary>
		/// Gets or sets the TabIndicatorColor of the TabStripControl instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public Color TabIndicatorColor {
			get{ return (Color)GetValue (TabIndicatorColorProperty); }
			set {
				SetValue (TabIndicatorColorProperty, value);
				_indicator.BackgroundColor = value;
			}
		}

		/// <summary>
		/// The TabHeight property.
		/// </summary>
		public static BindableProperty TabHeightProperty = 
			BindableProperty.Create(nameof(TabHeight), typeof(double), typeof(TabStripControl), 40.0,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.TabHeight = (double)newValue;
				});

		/// <summary>
		/// Gets or sets the TabHeight of the TabStripControl instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public double TabHeight {
			get{ return (double)GetValue (TabHeightProperty); }
			set {
				SetValue (TabHeightProperty, value);
			}
		}

		/// <summary>
		/// The TabBackColor property.
		/// </summary>
		public static BindableProperty TabBackColorProperty = 
			BindableProperty.Create(nameof(TabBackColor), typeof(Color), typeof(TabStripControl), Color.White,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.TabBackColor = (Color)newValue;
				});

		/// <summary>
		/// Gets or sets the TabBackColor of the TabStripControl instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public Color TabBackColor {
			get{ return (Color)GetValue (TabBackColorProperty); }
			set {
				SetValue (TabBackColorProperty, value);
				_tabControl.BackgroundColor = value;
			}
		}		

		/// <summary>
		/// The Shadow property.
		/// </summary>
		public static BindableProperty ShadowProperty = 
			BindableProperty.Create(nameof(Shadow), typeof(bool), typeof(TabStripControl), false,
				BindingMode.TwoWay,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabStripControl)bindable;
					ctrl.Shadow = (bool)newValue;
				});

		/// <summary>
		/// Gets or sets the Shadow of the TabControl instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public bool Shadow
		{
			get{ return (bool)GetValue(ShadowProperty); }
			set
			{
				SetValue(ShadowProperty, value);
				_shadow.IsVisible = value;
			}
		}
	}

	/// <summary>
	/// Tab child.
	/// </summary>
	public class TabItem
	{
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Title { get; set; }

		/// <summary>
		/// Gets the view.
		/// </summary>
		/// <value>The view.</value>
		public View View { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NControl.Controls.TabChild"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="view">View.</param>
		public TabItem()
		{			
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NControl.Controls.TabChild"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="view">View.</param>
		public TabItem(string title, View view)
		{
			Title = title;
			View = view;
		}
	}

	/// <summary>
	/// Tab bar indicator.
	/// </summary>
	public class TabBarIndicator: View
	{

	}

	/// <summary>
	/// Tab bar button.
	/// </summary>
	public class TabBarButton: NControlView
	{
		private readonly Label _label;

		public Color DarkTextColor = Color.Black;
		public Color AccentColor = Color.Accent;

		/// <summary>
		/// Initializes a new instance of the <see cref="DesaCo.TabBarButton"/> class.
		/// </summary>
		public TabBarButton()
		{
			_label = new Label {				
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,                
				FontSize = 14,
				LineBreakMode = LineBreakMode.TailTruncation,					
				TextColor = DarkTextColor,
			};

			Content = new StackLayout{
				Orientation = StackOrientation.Vertical,
				VerticalOptions = LayoutOptions.CenterAndExpand,
				HorizontalOptions = LayoutOptions.Center,
				Padding = 10,
			};

			(Content as StackLayout).Children.Add (_label);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Clooger.FormsApp.UserControls.TabBarButton"/> class.
		/// </summary>
		public TabBarButton(string buttonText): this()
		{			
			_label.Text = buttonText;
		}

		/// <summary>
		/// The ButtonText property.
		/// </summary>
		public static BindableProperty ButtonTextProperty = 
			BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(TabBarButton), null,
				defaultBindingMode: BindingMode.TwoWay,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabBarButton)bindable;
					ctrl.ButtonText = (string)newValue;
				});

		/// <summary>
		/// Gets or sets the ButtonText of the TabBarButton instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public string ButtonText {
			get{ return (string)GetValue (ButtonTextProperty); }
			set {
				SetValue (ButtonTextProperty, value);
				_label.Text = value;
			}
		}

		/// <summary>
		/// The SelectedColor property.
		/// </summary>
		public static BindableProperty SelectedColorProperty = 
			BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(TabBarButton), Color.Accent,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabBarButton)bindable;
					ctrl.SelectedColor = (Color)newValue;
				});

		/// <summary>
		/// Gets or sets the SelectedColor of the TabBarButton instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public Color SelectedColor {
			get{ return (Color)GetValue (SelectedColorProperty); }
			set {
				SetValue (SelectedColorProperty, value);
				AccentColor = value;
			}
		}

		/// <summary>
		/// The FontSize property.
		/// </summary>
		public static BindableProperty FontSizeProperty =
			BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TabBarButton), 14.0,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabBarButton)bindable;
					ctrl.FontSize = (double)newValue;
				});

		/// <summary>
		/// Gets or sets the FontSize of the TabBarButton instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public double FontSize
		{
			get { return (double)GetValue(FontSizeProperty); }
			set
			{
				SetValue(FontSizeProperty, value);
				_label.FontSize = value;
			}
		}

        /// <summary>
        /// The FontFamily property.
        /// </summary>
        public static BindableProperty FontFamilyProperty =
            BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(TabBarButton), null,
				propertyChanged: (bindable, oldValue, newValue) => {
					var ctrl = (TabBarButton)bindable;
					ctrl.FontFamily = (string)newValue;
				});

		/// <summary>
		/// Gets or sets the FontFamily of the TabBarButton instance.
		/// </summary>
		/// <value>The color of the buton.</value>
		public string FontFamily {
			get{ return (string)GetValue (FontFamilyProperty); }
			set {
				SetValue (FontFamilyProperty, value);
				_label.FontFamily = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is selected.
		/// </summary>
		/// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
		public bool IsSelected
		{
			get
			{
				return _label.TextColor == AccentColor;
			}
			set
			{
				if (value)
					_label.TextColor = AccentColor;
				else
					_label.TextColor = DarkTextColor;
			}
		}	

		/// <summary>
		/// Toucheses the began.
		/// </summary>
		/// <param name="points">Points.</param>
		public override bool TouchesBegan(System.Collections.Generic.IEnumerable<NGraphics.Point> points)
		{
			base.TouchesBegan(points);
			_label.TextColor = AccentColor;

			return false;
		}
	}

	/// <summary>
	/// Stack layout ex.
	/// </summary>
	internal class StackLayoutEx: StackLayout
	{
		/// <summary>
		/// Make sure we lay out so that we only use as much (or little) space as necessary for 
		/// each item
		/// </summary>
		/// <remarks>Implementors wishing to change the default behavior of a Layout should override this method. It is suggested to
		/// still call the base method and modify its calculated results.</remarks>
		protected override void LayoutChildren (double x, double y, double width, double height)
		{
			base.LayoutChildren (x, y, width, height);

			//var total = Children.Sum (t => t.Width);
			//var parentWidth = (Parent as View).Width;

			//if (total < parentWidth) {

			//	// We need more space
			//	var diff = (parentWidth - total)/Children.Count;

			//	var xoffset = 0.0;
			//	foreach (var child in Children) {
			//		child.Layout (new Rectangle (child.X + xoffset, child.Y, child.Width + diff, child.Height));
			//		xoffset += diff;
			//	}
			//}
		}
	}
}

	