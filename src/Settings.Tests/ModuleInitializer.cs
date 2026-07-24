public static class ModuleInitializer
{
    // The whole point of this project. OrderBySettings is process wide, so it cannot be
    // exercised alongside tests that expect redundant ordering to be allowed, which is why
    // these tests live in their own assembly.
    #region ThrowOnRedundantOrderBySetting

    [ModuleInitializer]
    public static void Initialize() =>
        OrderBySettings.ThrowOnRedundantOrderBy = true;

    #endregion
}
