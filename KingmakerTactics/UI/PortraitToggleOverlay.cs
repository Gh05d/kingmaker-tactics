namespace KingmakerTactics.UI {
    // Wrath's implementation hooks Owlcat's MVVM party view, which Kingmaker does not have.
    // Rebuilt on Kingmaker's legacy UI in sub-project 2; config flag ShowPortraitToggles stays.
    public static class PortraitToggleOverlay {
        public static void Sync(float delta) { }
        public static void Cleanup() { }
    }
}
