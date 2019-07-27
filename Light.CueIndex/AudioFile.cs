namespace Light.CueIndex
{
    /// <summary>
    /// This command is used to specify a data/audio file that will be written to the recorder.
    /// </summary>
    struct AudioFile
    {
        private string m_Filename;
        private FileType m_Filetype;

        public string Filename
        {
            get { return m_Filename; }
            set { m_Filename = value; }
        }

        /// <summary>
        /// BINARY - Intel binary file (least significant byte first)
        /// MOTOROLA - Motorola binary file (most significant byte first)
        /// AIFF - Audio AIFF file
        /// WAVE - Audio WAVE file
        /// MP3 - Audio MP3 file
        /// </summary>
        public FileType Filetype
        {
            get { return m_Filetype; }
            set { m_Filetype = value; }
        }

        public AudioFile(string filename, string filetype)
        {
            m_Filename = filename;

            switch (filetype.Trim().ToUpper())
            {
                case "BINARY":
                    m_Filetype = FileType.BINARY;
                    break;
                case "MOTOROLA":
                    m_Filetype = FileType.MOTOROLA;
                    break;
                case "AIFF":
                    m_Filetype = FileType.AIFF;
                    break;
                case "WAVE":
                    m_Filetype = FileType.WAVE;
                    break;
                case "MP3":
                    m_Filetype = FileType.MP3;
                    break;
                default:
                    m_Filetype = FileType.BINARY;
                    break;
            }
        }

        public AudioFile(string filename, FileType filetype)
        {
            m_Filename = filename;
            m_Filetype = filetype;
        }
    }

}
