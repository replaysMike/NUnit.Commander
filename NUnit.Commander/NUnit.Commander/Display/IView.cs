namespace NUnit.Commander.Display
{
    public interface IView
    {
        void Draw(ViewContext context, long ticks);
        void Deactivate();
    }
}
