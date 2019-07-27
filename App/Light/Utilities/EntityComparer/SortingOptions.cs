namespace Light.Utilities.EntityComparer
{
    /// <summary>
    /// All sorting options.
    /// </summary>
    public enum SortingOptions
    {
        /// <summary>
        /// Sort by alphabets, using ascending sorting.
        /// </summary>
        AlphabetAscending = 0,
        /// <summary>
        /// Sort by alphabets, using descending sorting.
        /// </summary>
        AlphabetDescending = 1,
        /// <summary>
        /// Sort by track ID, using ascending sorting. 
        /// </summary>
        /// <remarks>Only supported in album details pages.</remarks>
        TrackId = 2
    }
}
