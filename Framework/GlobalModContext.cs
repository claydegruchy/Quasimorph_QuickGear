using MGSC;

namespace QuasimorphHelloWorld.Framework
{
    /// <summary>
    /// Global context holder for easy access to mod context throughout the codebase.
    /// </summary>
    public static class GlobalModContext
    {
        private static IModContext _context;
        private static int _currentSlot = -1;

        public static IModContext Context => _context;
        public static int CurrentSlot => _currentSlot;

        public static void SetContext(IModContext context)
        {
            _context = context;
        }

        public static void SetCurrentSlot(int slot)
        {
            _currentSlot = slot;
        }

        public static void UpdateSlotFromSave()
        {
            if (_context == null)
                return;

            SavedGameMetadata meta = _context.State.Get<SavedGameMetadata>();
            _currentSlot = (meta != null) ? meta.Slot : -1;
        }
    }
}
