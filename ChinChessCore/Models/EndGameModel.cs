using IceTea.Pure.BaseModels;
using IceTea.Pure.Utils;

namespace ChinChessCore.Models
{
    public class EndGameModel : NotifyBase
    {
        public EndGameModel(string name, string datas, string steps)
        {
            Name = name.AssertArgumentNotNull(nameof(name));
            Datas = datas;
            Steps = steps;
        }

        /// <summary>
        /// 用于反序列化
        /// </summary>
        public EndGameModel()
        {
        }


        private string _name;
        public string Name
        {
            get => _name;
            set => _name = value.AssertArgumentNotNull(nameof(_datas));
        }

        private string _datas;
        public string Datas
        {
            get => _datas;
            set => _datas = value.AssertArgumentNotNull(nameof(_datas));
        }


        private string _steps;
        public string Steps
        {
            get => _steps;
            set => SetProperty<string>(ref _steps, value.AssertArgumentNotNull(nameof(_steps)));
        }
    }
}
