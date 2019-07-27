using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.CueIndex
{
    /// <summary>
    /// This command is used to specify indexes (or subindexes) within a track.
    /// Syntax:
    ///  INDEX [number] [mm:ss:ff]
    /// </summary>
    struct Index
    {
        //0-99
        int m_number;

        int m_minutes;
        int m_seconds;
        int m_frames;

        /// <summary>
        /// Index number (0-99)
        /// </summary>
        public int Number
        {
            get { return m_number; }
            set
            {
                if (value > 99)
                {
                    m_number = 99;
                }
                else if (value < 0)
                {
                    m_number = 0;
                }
                else
                {
                    m_number = value;
                }
            }
        }

        /// <summary>
        /// Possible values: 0-99
        /// </summary>
        public int Minutes
        {
            get { return m_minutes; }
            set
            {
                if (value > 99)
                {
                    m_minutes = 99;
                }
                else if (value < 0)
                {
                    m_minutes = 0;
                }
                else
                {
                    m_minutes = value;
                }
            }
        }

        /// <summary>
        /// Possible values: 0-59
        /// There are 60 seconds/minute
        /// </summary>
        public int Seconds
        {
            get { return m_seconds; }
            set
            {
                if (value >= 60)
                {
                    m_seconds = 59;
                }
                else if (value < 0)
                {
                    m_seconds = 0;
                }
                else
                {
                    m_seconds = value;
                }
            }
        }

        /// <summary>
        /// Possible values: 0-74
        /// There are 75 frames/second
        /// </summary>
        public int Frames
        {
            get { return m_frames; }
            set
            {
                if (value >= 75)
                {
                    m_frames = 74;
                }
                else if (value < 0)
                {
                    m_frames = 0;
                }
                else
                {
                    m_frames = value;
                }
            }
        }

        /// <summary>
        /// The Index of a track.
        /// </summary>
        /// <param name="number">Index number 0-99</param>
        /// <param name="minutes">Minutes (0-99)</param>
        /// <param name="seconds">Seconds (0-59)</param>
        /// <param name="frames">Frames (0-74)</param>
        public Index(int number, int minutes, int seconds, int frames)
        {
            m_number = number;

            m_minutes = minutes;
            m_seconds = seconds;
            m_frames = frames;
        }
    }
}
