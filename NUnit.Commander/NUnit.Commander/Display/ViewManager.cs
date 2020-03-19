using NUnit.Commander.Display.Views;
using System;
using System.Collections.Generic;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// Manages the current view
    /// </summary>
    public class ViewManager
    {
        public ViewPages View { get; private set; }
        private ViewContext _viewContext;
        private bool _isPaused;
        private readonly int maxViews = Enum.GetValues(typeof(ViewPages)).Length;
        private Dictionary<ViewPages, IView> _views = new Dictionary<ViewPages, IView>();

        public ViewManager(ViewContext viewContext, ViewPages defaultView)
        {
            _viewContext = viewContext;
            View = defaultView;
            _views.Add(ViewPages.ActiveTests, new ActiveTestsView());
            _views.Add(ViewPages.ReportPreview, new ReportPreviewView());
            _views.Add(ViewPages.Errors, new ErrorsView());
        }

        public void TogglePauseDisplay()
        {
            _isPaused = !_isPaused;
        }

        public void PauseDisplay()
        {
            _isPaused = true;
        }

        public void UnpauseDisplay()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Set the active view
        /// </summary>
        /// <param name="view"></param>
        public void SetView(ViewPages view)
        {
            var currentView = (int)View;
            _views[(ViewPages)currentView].Deactivate();
            View = view;
        }

        /// <summary>
        /// Go to the previous view
        /// </summary>
        public void PreviousView()
        {
            var currentView = (int)View;
            _views[(ViewPages)currentView].Deactivate();
            currentView--;
            if (currentView -1 < 0)
                currentView = maxViews - 1;
            SetView((ViewPages)currentView);
        }

        /// <summary>
        /// Go to the next view
        /// </summary>
        public void NextView()
        {
            var currentView = (int)View;
            _views[(ViewPages)currentView].Deactivate();

            currentView++;
            if (currentView + 1 > maxViews)
                currentView = 0;
            SetView((ViewPages)currentView);
        }

        internal void Draw(long ticks)
        {
            if (!_viewContext.AllowDrawActiveTests)
                return;
            if (!_isPaused)
            {
                _viewContext.Lock.Wait();
                try
                {
                    _views[View].Draw(_viewContext, ticks);
                }
                finally
                {
                    _viewContext.Lock.Release();
                    _viewContext.LastDrawTime = DateTime.Now;
                }
            }
        }
    }
}
