using ChinChessCore.Commands;
using ChinChessCore.Models;
using IceTea.Pure.BaseModels;
using IceTea.Pure.Contracts;
using IceTea.Pure.Extensions;
using IceTea.Pure.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChinChessCore.AutomationEngines
{
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8603 // 可能返回 null 引用。
#pragma warning disable CS8604 // 引用类型参数可能为 null。
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
    public class EleEyeEngine : StarterBase, IEleEyeEngine
    {
        private Process _engineProcess;
        private StreamWriter _inputStream;
        private StreamReader _outputStream;

        private volatile bool _isThinking;
        private DateTime _startThinkTime;
        private DateTime _endThinkTime;

        public event Action<MovePath> OnBestMoveReceived;

        protected override async Task StartAsyncCore()
        {
            string enginePath = Path.Combine(AppStatics.ExeDirectory, "Resources/EleEyes/eleeye.exe");
            AppUtils.Assert(enginePath.IsFileExists(), "引擎文件不存在");

            var startInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            var eleeyeProcessArr = Process.GetProcessesByName("eleeye");

            foreach (var item in eleeyeProcessArr)
            {
                item.Kill();
            }

            _engineProcess = new Process() { StartInfo = startInfo };
            _engineProcess.Start();

            _inputStream = _engineProcess.StandardInput;
            _outputStream = _engineProcess.StandardOutput;

            SendCommand("ucci");

            bool isReady = await IsStartedAsync();
            if (isReady)
            {
                SendCommand("setoption Randomness");

                this.InitData(string.Empty);

                ReadOutputAsync();
            }

            async Task<bool> IsStartedAsync()
            {
                string line = string.Empty;
                while (!(line = await _outputStream.ReadLineAsync()).IsNullOrBlank())
                {
                    Debug.WriteLine($"<< {line}");

                    if (line == "ucciok")
                    {
                        return true;
                    }
                }

                return false;
            }

            async Task ReadOutputAsync()
            {
                try
                {
                    string line;
                    while (_outputStream != null && (line = await _outputStream.ReadLineAsync()) != null)
                    {
                        Debug.WriteLine($"<< {line}");

                        if (line.StartsWith("bestmove"))
                        {
                            this._isThinking = false;
                            this._endThinkTime = DateTime.Now;

                            int durationMills = (int)_endThinkTime.Subtract(_startThinkTime).TotalMilliseconds;

                            if (durationMills < 100)
                            {
                                await Task.Delay(100 - durationMills);
                            }

                            var move = ParseBestMove(line);
                            OnBestMoveReceived?.Invoke(move);

                            MovePath ParseBestMove(string _line)
                            {
                                // 解析bestmove响应，例如: "bestmove h2e2"
                                var parts = _line.Split(' ');
                                var str = parts.Length >= 2 ? parts[1] : null;

                                return this.ConvertFrom(str);
                            }
                        }
                        else if (line.StartsWith("info"))
                        {
                        }
                    }
                }
                catch
                {
                    throw;
                    //System.Windows.MessageBox.Show($"象棋引擎出错:{ex.Message}");
                }
            }
        }
        public void InitData(string fen = "")
        {
            string commandStr = fen.IsNullOrBlank() ? "position startpos" : $"position fen {fen}";

            SendCommand(commandStr);
        }

        public void Move(IReadOnlyList<MoveCommand> moveCommands, int thinkMills = 3000)
        {
            string move = string.Empty;

            if (moveCommands.AssertNotNull(nameof(IReadOnlyList<MoveCommand>)).Count > 0)
            {
                var stringBuilder = new StringBuilder();
                for (int i = moveCommands.Count - 1; i >= 0; i--)
                {
                    var record = moveCommands[i];

                    var movePath = new MovePath(record.From, record.To);

                    stringBuilder.Append(this.ConvertTo(movePath));

                    if (i > 0)
                    {
                        stringBuilder.Append(' ');
                    }
                }

                move = stringBuilder.ToString();
            }

            this.SendCommand($"position startpos moves {move}");

            this.SendCommand($"go movetime {thinkMills}");

            this._isThinking = true;
            this._startThinkTime = DateTime.Now;

            // 很有可能一直思考，所以要及时停止
            Task.Run(async () =>
            {
                await Task.Delay(thinkMills);
                if (this._isThinking)
                {
                    this.Stop();
                }
            });
        }

        public void SendCommand(string command)
        {
            this.CheckDispose();

            if (_inputStream != null && !_inputStream.BaseStream.CanWrite)
                return;

            _inputStream.WriteLine(command);
            _inputStream.Flush();
            Debug.WriteLine($">> {command}");
        }

        public void Stop() => SendCommand("stop");

        protected override void DisposeCore()
        {
            base.DisposeCore();

            SendCommand("quit");
            Thread.Sleep(100);

            if (!_engineProcess.HasExited)
                _engineProcess.Kill();

            OnBestMoveReceived = null;

            _inputStream?.Close();
            _inputStream = null;
            _outputStream?.Close();
            _outputStream = null;
            _engineProcess?.Dispose();
            _engineProcess = null;
        }

        public MovePath ConvertFrom(string moveInfo)
        {
            // 执行引擎建议的移动
            // 解析移动字符串并更新棋盘
            AppUtils.Assert(moveInfo.IsNotNullAnd(s => s.Length >= 4), "字符串不合法");

            Position from = ParseCoordinate(moveInfo.Substring(0, 2));
            Position to = ParseCoordinate(moveInfo.Substring(2, 2));

            return new MovePath(from, to);

            Position ParseCoordinate(string coord)
            {
                AppUtils.Assert(coord.IsNotNullAnd(s => s.Length == 2), "字符串不合法");

                // 解析坐标字符串，例如"h2" -> Point(7, 7)
                int column = coord[0] - 'a';
                int row = 9 - (coord[1] - '0');

                return new Position(row, column);
            }
        }

        public string ConvertTo(MovePath movePath)
        {
            // 将坐标转换为引擎理解的移动格式，例如"h2e2"
            // 中国象棋坐标系统转换
            char fromFile = (char)('a' + movePath.From.Column);
            int fromRank = 9 - movePath.From.Row;
            char toFile = (char)('a' + movePath.To.Column);
            int toRank = 9 - movePath.To.Row;

            return $"{fromFile}{fromRank}{toFile}{toRank}";
        }
    }
}