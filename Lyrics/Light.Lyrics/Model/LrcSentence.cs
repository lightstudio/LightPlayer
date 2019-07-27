namespace Light.Lyrics.Model
{
    public class LrcSentence
    {
        public long Time;
        public string Content;

        public LrcSentence(long time, string content)
        {
            Time = time;
            Content = content;
        }

        public override string ToString()
        {
            return Time.ToString() + " ms, content: " + Content;
        }
    }
}
