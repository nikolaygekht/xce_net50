
      //------------------------------------------------------------------------
      //This is auto-generated code. Do NOT modify it.
      //Modify ./auto/colorscheme.xml and ./auto/colorscheme.xslt instead!!!
      //------------------------------------------------------------------------
      #pragma warning disable RCS1036 // Remove redundant empty-line.
      #pragma warning disable RCS1037 // Remove trailing white-space.
      #pragma warning disable RCS1171 // Simplify lazy initialization.
      #pragma warning disable RCS1085 // Use auto-implemented property.

      using System;
      using System.Collections.Generic;
      using System.Text;
      using CanvasColor = Gehtsoft.Xce.Conio.Drawing.CanvasColor;

      namespace Gehtsoft.Xce.Conio.Win
      {
      public interface IColorScheme
      {
      
        #region Window colors
        CanvasColor WindowBackground
        {
            get;
        }
        
        #endregion
        
        #region Menu colors
        CanvasColor MenuItem
        {
            get;
        }
        
        CanvasColor MenuItemSelected
        {
            get;
        }
        
        CanvasColor MenuDisabledItem
        {
            get;
        }
        
        CanvasColor MenuDisabledItemSelected
        {
            get;
        }
        
        CanvasColor MenuHotKey
        {
            get;
        }
        
        CanvasColor MenuHotKeySelected
        {
            get;
        }
        
        #endregion
        
        #region Dialog box colors
        CanvasColor DialogBorder
        {
            get;
        }
        
        #endregion
        
        #region Dialog box label colors
        CanvasColor DialogItemLabelColor
        {
            get;
        }
        
        CanvasColor DialogItemLabelHotKey
        {
            get;
        }
        
        #endregion
        
        #region Dialog box check box label colors
        CanvasColor DialogItemCheckBoxColor
        {
            get;
        }
        
        CanvasColor DialogItemCheckBoxColorFocused
        {
            get;
        }
        
        CanvasColor DialogItemCheckBoxHotKey
        {
            get;
        }
        
        CanvasColor DialogItemCheckBoxHotKeyFocused
        {
            get;
        }
        
        CanvasColor DialogItemCheckBoxColorDisabled
        {
            get;
        }
        
        #endregion
        
        #region Dialog box button colors
        CanvasColor DialogItemButtonColor
        {
            get;
        }
        
        CanvasColor DialogItemButtonColorDisabled
        {
            get;
        }
        
        CanvasColor DialogItemButtonColorFocused
        {
            get;
        }
        
        CanvasColor DialogItemButtonHotKey
        {
            get;
        }
        
        CanvasColor DialogItemButtonHotKeyFocused
        {
            get;
        }
        
        #endregion
        
        #region Dialog box edit color
        CanvasColor DialogItemEditColor
        {
            get;
        }
        
        CanvasColor DialogItemEditColorDisabled
        {
            get;
        }
        
        CanvasColor DialogItemEditColorFocused
        {
            get;
        }
        
        CanvasColor DialogItemEditSelection
        {
            get;
        }
        
        CanvasColor DialogItemEditSelectionFocused
        {
            get;
        }
        
        #endregion
        
        #region Dialog box listbox color
        CanvasColor DialogItemListBoxColor
        {
            get;
        }
        
        CanvasColor DialogItemListBoxColorDisabled
        {
            get;
        }
        
        CanvasColor DialogItemListBoxColorFocused
        {
            get;
        }
        
        CanvasColor DialogItemListBoxSelection
        {
            get;
        }
        
        CanvasColor DialogItemListBoxSelectionFocused
        {
            get;
        }
        
        CanvasColor DialogItemListBoxSelectionHighlighted
        {
            get;
        }
        
        CanvasColor DialogItemListBoxSelectionFocusedHighlighted
        {
            get;
        }
        
        #endregion
        
    }

    public class ColorScheme : IColorScheme
    {
        
        #region Window colors
        private CanvasColor mWindowBackground;
		
        public CanvasColor WindowBackground
        {
            get
            {
                return mWindowBackground;
            }
            set
            {
                mWindowBackground = value;
            }
        }
        
        #endregion
        
        #region Menu colors
        private CanvasColor mMenuItem;
		
        public CanvasColor MenuItem
        {
            get
            {
                return mMenuItem;
            }
            set
            {
                mMenuItem = value;
            }
        }
        
        private CanvasColor mMenuItemSelected;
		
        public CanvasColor MenuItemSelected
        {
            get
            {
                return mMenuItemSelected;
            }
            set
            {
                mMenuItemSelected = value;
            }
        }
        
        private CanvasColor mMenuDisabledItem;
		
        public CanvasColor MenuDisabledItem
        {
            get
            {
                return mMenuDisabledItem;
            }
            set
            {
                mMenuDisabledItem = value;
            }
        }
        
        private CanvasColor mMenuDisabledItemSelected;
		
        public CanvasColor MenuDisabledItemSelected
        {
            get
            {
                return mMenuDisabledItemSelected;
            }
            set
            {
                mMenuDisabledItemSelected = value;
            }
        }
        
        private CanvasColor mMenuHotKey;
		
        public CanvasColor MenuHotKey
        {
            get
            {
                return mMenuHotKey;
            }
            set
            {
                mMenuHotKey = value;
            }
        }
        
        private CanvasColor mMenuHotKeySelected;
		
        public CanvasColor MenuHotKeySelected
        {
            get
            {
                return mMenuHotKeySelected;
            }
            set
            {
                mMenuHotKeySelected = value;
            }
        }
        
        #endregion
        
        #region Dialog box colors
        private CanvasColor mDialogBorder;
		
        public CanvasColor DialogBorder
        {
            get
            {
                return mDialogBorder;
            }
            set
            {
                mDialogBorder = value;
            }
        }
        
        #endregion
        
        #region Dialog box label colors
        private CanvasColor mDialogItemLabelColor;
		
        public CanvasColor DialogItemLabelColor
        {
            get
            {
                return mDialogItemLabelColor;
            }
            set
            {
                mDialogItemLabelColor = value;
            }
        }
        
        private CanvasColor mDialogItemLabelHotKey;
		
        public CanvasColor DialogItemLabelHotKey
        {
            get
            {
                return mDialogItemLabelHotKey;
            }
            set
            {
                mDialogItemLabelHotKey = value;
            }
        }
        
        #endregion
        
        #region Dialog box check box label colors
        private CanvasColor mDialogItemCheckBoxColor;
		
        public CanvasColor DialogItemCheckBoxColor
        {
            get
            {
                return mDialogItemCheckBoxColor;
            }
            set
            {
                mDialogItemCheckBoxColor = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxColorFocused;
		
        public CanvasColor DialogItemCheckBoxColorFocused
        {
            get
            {
                return mDialogItemCheckBoxColorFocused;
            }
            set
            {
                mDialogItemCheckBoxColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxHotKey;
		
        public CanvasColor DialogItemCheckBoxHotKey
        {
            get
            {
                return mDialogItemCheckBoxHotKey;
            }
            set
            {
                mDialogItemCheckBoxHotKey = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxHotKeyFocused;
		
        public CanvasColor DialogItemCheckBoxHotKeyFocused
        {
            get
            {
                return mDialogItemCheckBoxHotKeyFocused;
            }
            set
            {
                mDialogItemCheckBoxHotKeyFocused = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxColorDisabled;
		
        public CanvasColor DialogItemCheckBoxColorDisabled
        {
            get
            {
                return mDialogItemCheckBoxColorDisabled;
            }
            set
            {
                mDialogItemCheckBoxColorDisabled = value;
            }
        }
        
        #endregion
        
        #region Dialog box button colors
        private CanvasColor mDialogItemButtonColor;
		
        public CanvasColor DialogItemButtonColor
        {
            get
            {
                return mDialogItemButtonColor;
            }
            set
            {
                mDialogItemButtonColor = value;
            }
        }
        
        private CanvasColor mDialogItemButtonColorDisabled;
		
        public CanvasColor DialogItemButtonColorDisabled
        {
            get
            {
                return mDialogItemButtonColorDisabled;
            }
            set
            {
                mDialogItemButtonColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemButtonColorFocused;
		
        public CanvasColor DialogItemButtonColorFocused
        {
            get
            {
                return mDialogItemButtonColorFocused;
            }
            set
            {
                mDialogItemButtonColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemButtonHotKey;
		
        public CanvasColor DialogItemButtonHotKey
        {
            get
            {
                return mDialogItemButtonHotKey;
            }
            set
            {
                mDialogItemButtonHotKey = value;
            }
        }
        
        private CanvasColor mDialogItemButtonHotKeyFocused;
		
        public CanvasColor DialogItemButtonHotKeyFocused
        {
            get
            {
                return mDialogItemButtonHotKeyFocused;
            }
            set
            {
                mDialogItemButtonHotKeyFocused = value;
            }
        }
        
        #endregion
        
        #region Dialog box edit color
        private CanvasColor mDialogItemEditColor;
		
        public CanvasColor DialogItemEditColor
        {
            get
            {
                return mDialogItemEditColor;
            }
            set
            {
                mDialogItemEditColor = value;
            }
        }
        
        private CanvasColor mDialogItemEditColorDisabled;
		
        public CanvasColor DialogItemEditColorDisabled
        {
            get
            {
                return mDialogItemEditColorDisabled;
            }
            set
            {
                mDialogItemEditColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemEditColorFocused;
		
        public CanvasColor DialogItemEditColorFocused
        {
            get
            {
                return mDialogItemEditColorFocused;
            }
            set
            {
                mDialogItemEditColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemEditSelection;
		
        public CanvasColor DialogItemEditSelection
        {
            get
            {
                return mDialogItemEditSelection;
            }
            set
            {
                mDialogItemEditSelection = value;
            }
        }
        
        private CanvasColor mDialogItemEditSelectionFocused;
		
        public CanvasColor DialogItemEditSelectionFocused
        {
            get
            {
                return mDialogItemEditSelectionFocused;
            }
            set
            {
                mDialogItemEditSelectionFocused = value;
            }
        }
        
        #endregion
        
        #region Dialog box listbox color
        private CanvasColor mDialogItemListBoxColor;
		
        public CanvasColor DialogItemListBoxColor
        {
            get
            {
                return mDialogItemListBoxColor;
            }
            set
            {
                mDialogItemListBoxColor = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxColorDisabled;
		
        public CanvasColor DialogItemListBoxColorDisabled
        {
            get
            {
                return mDialogItemListBoxColorDisabled;
            }
            set
            {
                mDialogItemListBoxColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxColorFocused;
		
        public CanvasColor DialogItemListBoxColorFocused
        {
            get
            {
                return mDialogItemListBoxColorFocused;
            }
            set
            {
                mDialogItemListBoxColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelection;
		
        public CanvasColor DialogItemListBoxSelection
        {
            get
            {
                return mDialogItemListBoxSelection;
            }
            set
            {
                mDialogItemListBoxSelection = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionFocused;
		
        public CanvasColor DialogItemListBoxSelectionFocused
        {
            get
            {
                return mDialogItemListBoxSelectionFocused;
            }
            set
            {
                mDialogItemListBoxSelectionFocused = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionHighlighted;
		
        public CanvasColor DialogItemListBoxSelectionHighlighted
        {
            get
            {
                return mDialogItemListBoxSelectionHighlighted;
            }
            set
            {
                mDialogItemListBoxSelectionHighlighted = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionFocusedHighlighted;
		
        public CanvasColor DialogItemListBoxSelectionFocusedHighlighted
        {
            get
            {
                return mDialogItemListBoxSelectionFocusedHighlighted;
            }
            set
            {
                mDialogItemListBoxSelectionFocusedHighlighted = value;
            }
        }
        
        #endregion
        

        public ColorScheme()
        {
        }
		
		public static IColorScheme Default => White;

        
        private static ColorScheme mWhite = null;

        public static IColorScheme White
        {
            get
            {
                if (mWhite == null)
                {
                    mWhite = new ColorScheme()
					{
                    WindowBackground = new CanvasColor(0x70),
                    MenuItem = new CanvasColor(0x80),
                    MenuItemSelected = new CanvasColor(0x70),
                    MenuDisabledItem = new CanvasColor(0x87),
                    MenuDisabledItemSelected = new CanvasColor(0x78),
                    MenuHotKey = new CanvasColor(0x8e),
                    MenuHotKeySelected = new CanvasColor(0x7e),
                    DialogBorder = new CanvasColor(0x80),
                    DialogItemLabelColor = new CanvasColor(0x80),
                    DialogItemLabelHotKey = new CanvasColor(0x8e),
                    DialogItemCheckBoxColor = new CanvasColor(0x80),
                    DialogItemCheckBoxColorFocused = new CanvasColor(0x80),
                    DialogItemCheckBoxHotKey = new CanvasColor(0x8e),
                    DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x8e),
                    DialogItemCheckBoxColorDisabled = new CanvasColor(0x87),
                    DialogItemButtonColor = new CanvasColor(0x80),
                    DialogItemButtonColorDisabled = new CanvasColor(0x87),
                    DialogItemButtonColorFocused = new CanvasColor(0x8f),
                    DialogItemButtonHotKey = new CanvasColor(0x8e),
                    DialogItemButtonHotKeyFocused = new CanvasColor(0x8e),
                    DialogItemEditColor = new CanvasColor(0x08),
                    DialogItemEditColorDisabled = new CanvasColor(0x07),
                    DialogItemEditColorFocused = new CanvasColor(0x0f),
                    DialogItemEditSelection = new CanvasColor(0x30),
                    DialogItemEditSelectionFocused = new CanvasColor(0x3f),
                    DialogItemListBoxColor = new CanvasColor(0x08),
                    DialogItemListBoxColorDisabled = new CanvasColor(0x07),
                    DialogItemListBoxColorFocused = new CanvasColor(0x0f),
                    DialogItemListBoxSelection = new CanvasColor(0x03),
                    DialogItemListBoxSelectionFocused = new CanvasColor(0xf3),
                    DialogItemListBoxSelectionHighlighted = new CanvasColor(0x0b),
                    DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0xfb),
                    
			        };
			}
			return mWhite;
			}
		}
		
        private static ColorScheme mBlue = null;

        public static IColorScheme Blue
        {
            get
            {
                if (mBlue == null)
                {
                    mBlue = new ColorScheme()
					{
                    WindowBackground = new CanvasColor(0x17),
                    MenuItem = new CanvasColor(0x3f),
                    MenuItemSelected = new CanvasColor(0x0f),
                    MenuDisabledItem = new CanvasColor(0x37),
                    MenuDisabledItemSelected = new CanvasColor(0x70),
                    MenuHotKey = new CanvasColor(0x3e),
                    MenuHotKeySelected = new CanvasColor(0x0e),
                    DialogBorder = new CanvasColor(0x30),
                    DialogItemLabelColor = new CanvasColor(0x30),
                    DialogItemLabelHotKey = new CanvasColor(0x3e),
                    DialogItemCheckBoxColor = new CanvasColor(0x30),
                    DialogItemCheckBoxColorFocused = new CanvasColor(0x30),
                    DialogItemCheckBoxHotKey = new CanvasColor(0x3e),
                    DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x3e),
                    DialogItemCheckBoxColorDisabled = new CanvasColor(0x37),
                    DialogItemButtonColor = new CanvasColor(0x30),
                    DialogItemButtonColorDisabled = new CanvasColor(0x37),
                    DialogItemButtonColorFocused = new CanvasColor(0x3f),
                    DialogItemButtonHotKey = new CanvasColor(0x3e),
                    DialogItemButtonHotKeyFocused = new CanvasColor(0x3e),
                    DialogItemEditColor = new CanvasColor(0x03),
                    DialogItemEditColorDisabled = new CanvasColor(0x07),
                    DialogItemEditColorFocused = new CanvasColor(0x0b),
                    DialogItemEditSelection = new CanvasColor(0x10),
                    DialogItemEditSelectionFocused = new CanvasColor(0x1f),
                    DialogItemListBoxColor = new CanvasColor(0x03),
                    DialogItemListBoxColorDisabled = new CanvasColor(0x07),
                    DialogItemListBoxColorFocused = new CanvasColor(0x0b),
                    DialogItemListBoxSelection = new CanvasColor(0x10),
                    DialogItemListBoxSelectionFocused = new CanvasColor(0x1f),
                    DialogItemListBoxSelectionHighlighted = new CanvasColor(0x1e),
                    DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0x0e),
                    
			        };
			}
			return mBlue;
			}
		}
		
        private static ColorScheme mBlack = null;

        public static IColorScheme Black
        {
            get
            {
                if (mBlack == null)
                {
                    mBlack = new ColorScheme()
					{
                    WindowBackground = new CanvasColor(0x07),
                    MenuItem = new CanvasColor(0x0f),
                    MenuItemSelected = new CanvasColor(0x70),
                    MenuDisabledItem = new CanvasColor(0x07),
                    MenuDisabledItemSelected = new CanvasColor(0x78),
                    MenuHotKey = new CanvasColor(0x0e),
                    MenuHotKeySelected = new CanvasColor(0x7e),
                    DialogBorder = new CanvasColor(0x0f),
                    DialogItemLabelColor = new CanvasColor(0x07),
                    DialogItemLabelHotKey = new CanvasColor(0x0e),
                    DialogItemCheckBoxColor = new CanvasColor(0x07),
                    DialogItemCheckBoxColorFocused = new CanvasColor(0x0f),
                    DialogItemCheckBoxHotKey = new CanvasColor(0x0e),
                    DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x0e),
                    DialogItemCheckBoxColorDisabled = new CanvasColor(0x08),
                    DialogItemButtonColor = new CanvasColor(0x07),
                    DialogItemButtonColorDisabled = new CanvasColor(0x08),
                    DialogItemButtonColorFocused = new CanvasColor(0x0f),
                    DialogItemButtonHotKey = new CanvasColor(0x0e),
                    DialogItemButtonHotKeyFocused = new CanvasColor(0x0e),
                    DialogItemEditColor = new CanvasColor(0x70),
                    DialogItemEditColorDisabled = new CanvasColor(0x78),
                    DialogItemEditColorFocused = new CanvasColor(0x70),
                    DialogItemEditSelection = new CanvasColor(0x10),
                    DialogItemEditSelectionFocused = new CanvasColor(0x1f),
                    DialogItemListBoxColor = new CanvasColor(0x70),
                    DialogItemListBoxColorDisabled = new CanvasColor(0x78),
                    DialogItemListBoxColorFocused = new CanvasColor(0x7f),
                    DialogItemListBoxSelection = new CanvasColor(0x10),
                    DialogItemListBoxSelectionFocused = new CanvasColor(0x1f),
                    DialogItemListBoxSelectionHighlighted = new CanvasColor(0x7e),
                    DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0x1e),
                    
			        };
			}
			return mBlack;
			}
		}
		
    }
}
    