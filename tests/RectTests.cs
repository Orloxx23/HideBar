using HideBar.Core;

namespace HideBar.Tests
{
    static class RectTests
    {
        static readonly Rect Screen = new Rect(0, 0, 1920, 1080);

        public static void Register()
        {
            TestRunner.Add("a bottom taskbar takes space from the bottom", BottomTaskbar);
            TestRunner.Add("a top taskbar takes space from the top", TopTaskbar);
            TestRunner.Add("a left taskbar takes space from the left", LeftTaskbar);
            TestRunner.Add("a right taskbar takes space from the right", RightTaskbar);
        }

        static void BottomTaskbar()
        {
            Assert.Equal(new Rect(0, 0, 1920, 1032), Screen.Without(new Rect(0, 1032, 1920, 1080)), "work area");
        }

        static void TopTaskbar()
        {
            Assert.Equal(new Rect(0, 48, 1920, 1080), Screen.Without(new Rect(0, 0, 1920, 48)), "work area");
        }

        static void LeftTaskbar()
        {
            Assert.Equal(new Rect(62, 0, 1920, 1080), Screen.Without(new Rect(0, 0, 62, 1080)), "work area");
        }

        static void RightTaskbar()
        {
            Assert.Equal(new Rect(0, 0, 1858, 1080), Screen.Without(new Rect(1858, 0, 1920, 1080)), "work area");
        }
    }
}
