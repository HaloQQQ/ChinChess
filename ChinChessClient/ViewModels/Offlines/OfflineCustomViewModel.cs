using ChinChessClient.Contracts;
using ChinChessClient.Models;
using ChinChessCore.Models;
using IceTea.Pure.Utils;
using IceTea.Wpf.Atom.Utils.HotKey.App;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChinChessClient.ViewModels;

#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
internal class OfflineCustomViewModel : OfflineChinChessViewModelBase
{
    public override ChinChessMode Mode => ChinChessMode.OfflineCustom;

    public IList<CustomChess> TemplateDatas { get; private set; } = new ObservableCollection<CustomChess>();

    public OfflineCustomViewModel(IAppConfigFileHotKeyManager appCfgHotKeyManager)
        : base(appCfgHotKeyManager)
    {
        this.Status = GameStatus.NotInitialized;

        CustomOverCommand = new DelegateCommand(() =>
                                {
                                    if (this.Datas.Count(d => d.Data.Type == ChessType.帥) != 2)
                                    {
                                        this.PublishMsg("必须要设置将帅");
                                        return;
                                    }

                                    var enemyShuai = this.Datas.First(d => d.Data.Type == ChessType.帥 && d.Data.IsRed != this.IsRedTurn);

                                    if (enemyShuai.Data.IsDangerous(this._canPutVisitor, enemyShuai.Pos, out _) ||
                                        _canPutVisitor.FaceToFace() ||
                                        this.IsGameOver())
                                    {
                                        this.PublishMsg("不允许设计死局");
                                        return;
                                    }

                                    IsCustomOver = true;
                                    this.Status = GameStatus.Ready;
                                })
                                .ObservesProperty(() => IsCustomOver);

        SelectOrPutCommand = new DelegateCommand<ChinChessModel>(
            SelectOrPut_CommandExecute,
            model => IsCustomOver && this.Status == GameStatus.Ready && model != null && CurrentChess != model
        )
        .ObservesProperty(() => this.Status)
        .ObservesProperty(() => this.IsCustomOver)
        .ObservesProperty(() => this.IsRedTurn)
        .ObservesProperty(() => this.CurrentChess);
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
        TemplateDatas.Clear();

        TemplateDatas.Add(new CustomChess(ChessType.車, true, 2));
        TemplateDatas.Add(new CustomChess(ChessType.馬, true, 2));
        TemplateDatas.Add(new CustomChess(ChessType.相, true, 2));
        TemplateDatas.Add(new CustomChess(ChessType.仕, true, 2));
        TemplateDatas.Add(new CustomChess(ChessType.帥, true, 1));
        TemplateDatas.Add(new CustomChess(ChessType.炮, true, 2));
        TemplateDatas.Add(new CustomChess(ChessType.兵, true, 5));

        TemplateDatas.Add(new CustomChess(ChessType.車, false, 2));
        TemplateDatas.Add(new CustomChess(ChessType.馬, false, 2));
        TemplateDatas.Add(new CustomChess(ChessType.相, false, 2));
        TemplateDatas.Add(new CustomChess(ChessType.仕, false, 2));
        TemplateDatas.Add(new CustomChess(ChessType.帥, false, 1));
        TemplateDatas.Add(new CustomChess(ChessType.炮, false, 2));
        TemplateDatas.Add(new CustomChess(ChessType.兵, false, 5));

        foreach (var item in this.Datas)
        {
            item.Dispose();
        }
        this.Datas.Clear();

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                this.Datas.Add(new ChinChessModel(i, j));
            }
        }

        IsCustomOver = false;
    }

    private bool _isCustomOver;
    public bool IsCustomOver
    {
        get => _isCustomOver;
        private set => SetProperty<bool>(ref _isCustomOver, value);
    }

    public ICommand CustomOverCommand { get; private set; }

    protected override void DisposeCore()
    {
        base.DisposeCore();

        this.CustomOverCommand = null;

        this.TemplateDatas.Clear();
        this.TemplateDatas = null;
    }
}
