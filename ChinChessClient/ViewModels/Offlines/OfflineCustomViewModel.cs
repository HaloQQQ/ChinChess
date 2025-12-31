using ChinChessClient.Contracts.Events;
using ChinChessCore.Contracts;
using ChinChessCore.Models;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChinChessClient.ViewModels;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8629 // 可为 null 的值类型可为 null。
internal class OfflineCustomViewModel : OfflineChinChessViewModelBase
{
    private IEventAggregator _eventAggregator;

    public override ChinChessMode Mode => ChinChessMode.OfflineCustom;

    private EndGameModel _endGameModel;

    public IList<CustomChess> TemplateDatas { get; private set; } = new ObservableCollection<CustomChess>();

    public OfflineCustomViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager, IConfigManager configManager, IEventAggregator eventAggregator)
        : base(appCfgHotKeyManager)
    {
        _eventAggregator = eventAggregator;

        this.Status = EnumGameStatus.NotInitialized;

        DesignOverCommand = new DelegateCommand(() =>
                                {
                                    if (!CanUse())
                                    {
                                        return;
                                    }

                                    IsDesignOver = true;
                                }, () => !IsDesignOver)
                                .ObservesProperty(() => IsDesignOver);

        SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            model => SelectOrPut_CommandExecute(model),
            model => IsDesignOver && this.Status == EnumGameStatus.Ready
                    && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsDesignOver)
        .ObservesProperty(() => this.CurrentChess);

        SaveDesignCommand = new DelegateCommand<string>(name =>
        {
            if (!CanUse())
            {
                return;
            }

            IsDesignOver = true;

            var chessesInfo = this.Datas.Where(d => !d.Data.IsEmpty)
                                .Select(m => new ChinChessInfo(m.Pos, isRed: (bool)m.Data.IsRed, chessType: (ChessType)m.Data.Type))
                                .ToArray();

            var infoStr = ChinChessSerializer.Serialize(chessesInfo);

            _endGameModel = new EndGameModel(name, infoStr);

            configManager.WriteConfigNode<EndGameModel>(_endGameModel, ["EndGames", name]);

            IsSaving = false;

            DesignName = string.Empty;

            this.PublishMsg("已保存布局");

            eventAggregator.GetEvent<MainTitleChangedEvent>().Publish(name);
        }).ObservesCanExecute(() => IsSaving);

        this.SaveRecordCommand = new DelegateCommand(() =>
        {
            IEnumerable<string> steps = this.CommandStack.OrderBy(c => c.Index).Select(c => c.Notation);

            _endGameModel.Steps = string.Join(',', steps);

            configManager.WriteConfigNode<EndGameModel>(_endGameModel, ["EndGames", _endGameModel.Name]);

            this.Log(this.Name, "保存行棋步骤", this.IsRedTurn);

            this.PublishMsg("已保存行棋步骤");

            if (!this.IsGameOver())
            {
                this.Result = EnumGameResult.Deuce;
            }
        }, () => Result == EnumGameResult.During && !this.CommandStack.IsNullOrEmpty())
            .ObservesProperty(() => this.CommandStack.Count)
            .ObservesProperty(() => Result);

        bool CanUse()
        {
            if (this.Datas.Count(d => d.Data.Type == ChessType.帥) != 2)
            {
                this.PublishMsg("必须要设置将帅");
                return false;
            }

            var enemyShuai = this.Datas.First(d => d.Data.Type == ChessType.帥 && d.Data.IsRed != this.IsRedTurn);

            if (enemyShuai.Data.IsDangerous(this._canPutVisitor, enemyShuai.Pos, out _) 
                || _canPutVisitor.FaceToFace() 
                || this.IsGameOver())
            {
                this.PublishMsg("不允许设计死局");
                return false;
            }

            return true;
        }
    }

    public void ReturnData(ChinChessModel chinChessModel)
    {
        chinChessModel.AssertArgumentNotNull(nameof(chinChessModel));

        TemplateDatas.First(d => d.Type == chinChessModel.Data.Type && d.IsRed == chinChessModel.Data.IsRed).Increase();

        this.Datas[chinChessModel.Pos.Index].Data = InnerChinChess.Empty;
    }

    public void SetData(Position pos, CustomChess data)
    {
        data.AssertArgumentNotNull(nameof(data));

        InnerChinChess chess = default;
        switch (data.Type)
        {
            case ChessType.炮:
                chess = new ChinChessPao(data.IsRed);
                break;
            case ChessType.兵:
                chess = new ChinChessBing(data.IsRed);
                break;
            case ChessType.車:
                chess = new ChinChessJu(data.IsRed);
                break;
            case ChessType.馬:
                chess = new ChinChessMa(data.IsRed);
                break;
            case ChessType.相:
                chess = new ChinChessXiang(data.IsRed);
                break;
            case ChessType.仕:
                chess = new ChinChessShi(data.IsRed);
                break;
            case ChessType.帥:
                chess = new ChinChessShuai(data.IsRed);
                break;
            default:
                break;
        }

        if (chess.IsPosValid(pos))
        {
            this.Datas[pos.Index].Data = chess;

            data.Decrease();
        }
    }

    protected override void InitDatas()
    {
        foreach (var item in this.Datas)
        {
            item.Dispose();
        }
        this.Datas.Clear();

        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 9; column++)
            {
                this.Datas.Add(new ChinChessModel(row, column));
            }
        }

        if (_endGameModel.IsNotNullAnd(_ => !_.Datas.IsNullOrBlank()))
        {
            var list = ChinChessSerializer.Deserialize(_endGameModel.Datas);

            foreach (var item in list)
            {
                this.Datas[item.Pos.Index].Reload(item);
            }
        }

        TemplateDatas.Clear();

        // 计算扣除已存在棋子后的模板数据
        var templateDataList = CalculateTemplateDatas();
        foreach (var templateData in templateDataList)
        {
            TemplateDatas.Add(templateData);
        }

        IsDesignOver = false;
    }

    /// <summary>
    /// 计算扣除已存在棋子后的模板数据
    /// </summary>
    /// <returns>扣除后的棋子模板列表</returns>
    private List<CustomChess> CalculateTemplateDatas()
    {
        // 定义每种棋子的默认数量
        var defaultCounts = new Dictionary<(ChessType, bool), int>
        {
            { (ChessType.車, true), 2 },
            { (ChessType.馬, true), 2 },
            { (ChessType.相, true), 2 },
            { (ChessType.仕, true), 2 },
            { (ChessType.帥, true), 1 },
            { (ChessType.炮, true), 2 },
            { (ChessType.兵, true), 5 },
            { (ChessType.車, false), 2 },
            { (ChessType.馬, false), 2 },
            { (ChessType.相, false), 2 },
            { (ChessType.仕, false), 2 },
            { (ChessType.帥, false), 1 },
            { (ChessType.炮, false), 2 },
            { (ChessType.兵, false), 5 }
        };

        // 统计棋盘上已存在的棋子数量
        var existingCounts = new Dictionary<(ChessType, bool), int>();

        // 初始化计数器
        foreach (var key in defaultCounts.Keys)
        {
            existingCounts[key] = 0;
        }

        // 统计棋盘上已存在的棋子
        foreach (var data in this.Datas)
        {
            if (!data.Data.IsEmpty)
            {
                var key = ((ChessType)data.Data.Type, (bool)data.Data.IsRed);

                if (existingCounts.ContainsKey(key))
                {
                    existingCounts[key]++;
                }
            }
        }

        // 计算并返回TemplateDatas（默认数量 - 已存在数量）
        var result = new List<CustomChess>();
        foreach (var (key, defaultCount) in defaultCounts)
        {
            var existingCount = existingCounts[key];
            var remainingCount = Math.Max(0, defaultCount - existingCount);

            if (remainingCount > 0)
            {
                result.Add(new CustomChess(key.Item1, key.Item2, remainingCount));
            }
        }

        return result;
    }

    protected override void RePlay_CommandExecute()
    {
        base.RePlay_CommandExecute();

        this._eventAggregator.GetEvent<MainTitleChangedEvent>().Publish(this.Title);
    }

    private bool _isDesignOver;
    public bool IsDesignOver
    {
        get => _isDesignOver;
        private set
        {
            if (SetProperty<bool>(ref _isDesignOver, value))
            {
                if (value)
                {
                    this.Status = EnumGameStatus.Ready;
                }
            }
        }
    }


    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty<bool>(ref _isSaving, value);
    }

    private string _designName;
    public string DesignName
    {
        get => _designName;
        set => SetProperty<string>(ref _designName, value);
    }

    public ICommand DesignOverCommand { get; private set; }
    public ICommand SaveDesignCommand { get; private set; }
    public ICommand SaveRecordCommand { get; private set; }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        var parameters = navigationContext.Parameters;

        if (parameters.TryGetValue<EndGameModel>("EndGame", out EndGameModel data))
        {
            _endGameModel = data;

            foreach (var item in this.Datas)
            {
                item.Dispose();
            }

            this.Datas.Clear();

            this.InitDatas();

            this.DesignOverCommand.Execute(null);
        }
    }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        this.DesignOverCommand = null;
        this.SaveDesignCommand = null;
        this.SaveRecordCommand = null;

        this._endGameModel = null;

        this._eventAggregator = null;

        this.TemplateDatas.Clear();
        this.TemplateDatas = null;
    }
}
