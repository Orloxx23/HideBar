namespace HideBar.Core
{
    // A screen rectangle in physical pixels.
    struct Rect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }

        // This rectangle minus the screen edge a taskbar covers.
        public Rect Without(Rect taskbar)
        {
            int left = Left, top = Top, right = Right, bottom = Bottom;
            if (taskbar.Width >= taskbar.Height)
            {
                if (taskbar.Bottom >= Bottom && taskbar.Top > Top) bottom = taskbar.Top;
                else if (taskbar.Top <= Top) top = taskbar.Bottom;
            }
            else
            {
                if (taskbar.Left <= Left) left = taskbar.Right;
                else if (taskbar.Right >= Right) right = taskbar.Left;
            }
            return new Rect(left, top, right, bottom);
        }

        public bool Contains(Rect other)
        {
            return other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Rect)) return false;
            Rect other = (Rect)obj;
            return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
        }

        public override int GetHashCode()
        {
            return Left ^ (Top << 8) ^ (Right << 16) ^ (Bottom << 24);
        }

        public static bool operator ==(Rect a, Rect b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Rect a, Rect b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})-({2},{3})", Left, Top, Right, Bottom);
        }
    }
}
