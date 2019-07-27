namespace Light.CueIndex
{
    /// <summary>
    /// BINARY - Intel binary file (least significant byte first)
    /// MOTOROLA - Motorola binary file (most significant byte first)
    /// AIFF - Audio AIFF file
    /// WAVE - Audio WAVE file
    /// MP3 - Audio MP3 file
    /// </summary>
    enum FileType
    {
        BINARY, MOTOROLA, AIFF, WAVE, MP3
    }
}
