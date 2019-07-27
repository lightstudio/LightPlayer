using GalaSoft.MvvmLight;

namespace Light.Model
{
    public class BuildInfo : ViewModelBase
    {
        private string _buildEnv;
        private string _branch;
        private string _type;
        private string _versionString;

        public string BuildEnv
        {
            get { return _buildEnv; }
            set
            {
                _buildEnv = value;
                RaisePropertyChanged();
            }
        }

        public string Branch
        {
            get { return _branch; }
            set
            {
                _branch = value;
                RaisePropertyChanged();
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                RaisePropertyChanged();
            }
        }

        public string VersionString
        {
            get { return _versionString; }
            set
            {
                _versionString = value;
                RaisePropertyChanged();
            }
        }
    }

}
