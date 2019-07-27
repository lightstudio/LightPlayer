namespace Light.CueIndex
{
    /// <summary>
    ///DCP - Digital copy permitted
    ///4CH - Four channel audio
    ///PRE - Pre-emphasis enabled (audio tracks only)
    ///SCMS - Serial copy management system (not supported by all recorders)
    ///There is a fourth subcode flag called "DATA" which is set for all non-audio tracks. This flag is set automatically based on the datatype of the track.
    /// </summary>
    enum Flags
    {
        DCP, CH4, PRE, SCMS, DATA, NONE
    }
}
