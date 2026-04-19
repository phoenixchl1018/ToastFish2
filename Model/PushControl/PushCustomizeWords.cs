using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToastFish.Model.Log;
using ToastFish.Model.SqliteControl;
using System.Threading;

namespace ToastFish.Model.PushControl
{
    /// <summary>
    /// 自定义词库推送类 - 支持背诵和测试两种模式
    /// </summary>
    class PushCustomizeWords : PushWords
    {
        #region 原有的背诵模式方法

        /// <summary>
        /// 推送一个自定义单词（背诵模式）
        /// </summary>
        public static void PushOneWord(CustomizeWord CurrentWord)
        {
            new ToastContentBuilder()
            .AddText(CurrentWord.firstLine + '\n' + CurrentWord.secondLine)
            .AddText(CurrentWord.thirdLine + '\n' + CurrentWord.fourthLine)

            .AddButton(new ToastButton()
                .SetContent("记住了！")
                .AddArgument("action", "succeed")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent("暂时跳过..")
                .AddArgument("action", "fail")
                .SetBackgroundActivation())

            .Show();
        }

        /// <summary>
        /// 背诵模式入口
        /// </summary>
        public static new void Recitation(Object Words)
        {
            WordType WordList = (WordType)Words;
            PushCustomizeWords pushCustomizeWords = new PushCustomizeWords();
            List<CustomizeWord> RandomList = new List<CustomizeWord>();

            if (WordList.CustWordList != null)
            {
                RandomList = WordList.CustWordList;
                // 同时保存到静态列表，供测试使用
                Select.SetCustomWordList(WordList.CustWordList);
            }

            if (RandomList.Count == 0)
                return;

            CustomizeWord CurrentWord = new CustomizeWord();

            while (RandomList.Count != 0)
            {
                CurrentWord = RandomList[0];
                PushOneWord(CurrentWord);

                pushCustomizeWords.WORD_CURRENT_STATUS = 2;
                while (pushCustomizeWords.WORD_CURRENT_STATUS == 2)
                {
                    var task = pushCustomizeWords.ProcessToastNotificationRecitation();
                    if (task.Result == 0)
                    {
                        pushCustomizeWords.WORD_CURRENT_STATUS = 1;
                    }
                    else if (task.Result == 1)
                    {
                        pushCustomizeWords.WORD_CURRENT_STATUS = 0;
                    }
                }

                RandomList.Remove(CurrentWord);
                if (pushCustomizeWords.WORD_CURRENT_STATUS == 0)
                    RandomList.Add(CurrentWord);
            }
            pushCustomizeWords.PushMessage("背完了！");
        }

        #endregion

        #region 四选一测试模式

        /// <summary>
        /// 当前正确答案的索引（0=A, 1=B, 2=C, 3=D）
        /// </summary>
        public int CUSTOM_QUESTION_CURRENT_RIGHT_ANSWER = 0;

        /// <summary>
        /// 推送一道自定义四选一选择题
        /// 题干使用 firstLine，正确答案使用 thirdLine
        /// </summary>
        /// <param name="CurrentWord">当前题目</param>
        /// <param name="Fake1">干扰项1的答案</param>
        /// <param name="Fake2">干扰项2的答案</param>
        /// <param name="Fake3">干扰项3的答案</param>
        public void PushCustomTransQuestion(CustomizeWord CurrentWord, string Fake1, string Fake2, string Fake3)
        {
            string Question = CurrentWord.firstLine;  // 题干
            string CorrectAnswer = CurrentWord.thirdLine;  // 正确答案

            // 如果 thirdLine 为空，尝试使用 secondLine
            if (string.IsNullOrEmpty(CorrectAnswer))
                CorrectAnswer = CurrentWord.secondLine;

            // 如果都为空，使用 fourthLine
            if (string.IsNullOrEmpty(CorrectAnswer))
                CorrectAnswer = CurrentWord.fourthLine;

            Random Rd = new Random();
            int AnswerIndex = Rd.Next(4);  // 四个选项
            CUSTOM_QUESTION_CURRENT_RIGHT_ANSWER = AnswerIndex;

            string[] options = new string[4];
            options[AnswerIndex] = CorrectAnswer;
            
            // 填充干扰项
            int fakeIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i != AnswerIndex)
                {
                    if (fakeIndex == 0) options[i] = Fake1;
                    else if (fakeIndex == 1) options[i] = Fake2;
                    else options[i] = Fake3;
                    fakeIndex++;
                }
            }

            new ToastContentBuilder()
            .AddText("题目\n" + Question)
            .AddButton(new ToastButton()
                .SetContent("A. " + options[0])
                .AddArgument("action", "0")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("B. " + options[1])
                .AddArgument("action", "1")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("C. " + options[2])
                .AddArgument("action", "2")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("D. " + options[3])
                .AddArgument("action", "3")
                .SetBackgroundActivation())
            .Show();
        }

        /// <summary>
        /// 四选一测试模式入口
        /// </summary>
        /// <param name="Num">测试题目数量</param>
        public void UnorderCustomWord(Object Num)
        {
            int Number = (int)Num;
            Select Query = new Select();
            
            // 从自定义词库中抽取题目
            List<CustomizeWord> TestList = Query.GetRandomCustomWords(Number);

            if (TestList == null || TestList.Count == 0)
            {
                PushMessage("请先导入自定义词库！");
                return;
            }

            // 创建日志
            CreateLog Log = new CreateLog();
            String LogName = "Log\\" + DateTime.Now.ToString().Replace('/', '-').Replace(' ', '_').Replace(':', '-') + "_自定义词库测试.xlsx";
            Log.OutputExcel(LogName, TestList, "自定义");

            CustomizeWord CurrentWord = new CustomizeWord();
            Dictionary<string, string> AnswerDict = new Dictionary<string, string>()
            {
                {"0", "A"}, {"1", "B"}, {"2", "C"}, {"3", "D"}
            };

            while (TestList.Count != 0)
            {
                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(500);
                
                CurrentWord = GetRandomCustomWord(TestList);
                
                // 获取干扰项（排除当前题目）
                List<CustomizeWord> FakeWordList = GetFakeCustomWords(Query, CurrentWord, 3);

                // 如果干扰项不足，使用默认值
                while (FakeWordList.Count < 3)
                {
                    CustomizeWord fakeWord = new CustomizeWord();
                    fakeWord.thirdLine = "干扰项" + (FakeWordList.Count + 1);
                    FakeWordList.Add(fakeWord);
                }

                PushCustomTransQuestion(CurrentWord, 
                    FakeWordList[0].thirdLine, 
                    FakeWordList[1].thirdLine, 
                    FakeWordList[2].thirdLine);

                QUESTION_CURRENT_STATUS = 2;
                while (QUESTION_CURRENT_STATUS == 2)
                {
                    var task = ProcessToastNotificationQuestion();
                    if (task.Result == 1)
                        QUESTION_CURRENT_STATUS = 1;
                    else if (task.Result == 0)
                        QUESTION_CURRENT_STATUS = 0;
                    else if (task.Result == -1)
                        QUESTION_CURRENT_STATUS = -1;
                }

                if (QUESTION_CURRENT_STATUS == 1)
                {
                    TestList.Remove(CurrentWord);
                    Thread.Sleep(500);
                }
                else if (QUESTION_CURRENT_STATUS == 0)
                {
                    string correctAnswer = CurrentWord.thirdLine;
                    if (string.IsNullOrEmpty(correctAnswer))
                        correctAnswer = CurrentWord.secondLine;
                    
                    new ToastContentBuilder()
                    .AddText("错误！正确答案：" + AnswerDict[CUSTOM_QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + ". " + correctAnswer)
                    .Show();
                    Thread.Sleep(3000);
                }
            }
            
            ToastNotificationManagerCompat.History.Clear();
            PushMessage("测试结束！恭喜完成！");
        }

        /// <summary>
        /// 从列表中随机获取一个自定义单词
        /// </summary>
        private CustomizeWord GetRandomCustomWord(List<CustomizeWord> WordList)
        {
            Random Rd = new Random();
            int Index = Rd.Next(WordList.Count);
            return WordList[Index];
        }

        /// <summary>
        /// 获取干扰项（排除当前题目）
        /// </summary>
        private List<CustomizeWord> GetFakeCustomWords(Select Query, CustomizeWord CurrentWord, int Count)
        {
            List<CustomizeWord> Result = new List<CustomizeWord>();
            List<CustomizeWord> AllWords = Select.CustomWordListForTest;
            
            if (AllWords == null || AllWords.Count <= 1)
                return Result;

            // 过滤掉当前题目
            List<CustomizeWord> AvailableWords = AllWords
                .Where(w => w.firstLine != CurrentWord.firstLine)
                .ToList();

            Random Rd = new Random();
            int actualCount = Math.Min(Count, AvailableWords.Count);
            
            for (int i = 0; i < actualCount; i++)
            {
                int Index = Rd.Next(AvailableWords.Count);
                Result.Add(AvailableWords[Index]);
                AvailableWords.RemoveAt(Index);
            }

            return Result;
        }

        #endregion

        #region 闪卡翻转测试模式（适用于问答题等非单词类型）

        /// <summary>
        /// 闪卡模式 - 显示题目
        /// </summary>
        public void PushFlashCardQuestion(CustomizeWord CurrentWord)
        {
            new ToastContentBuilder()
            .AddText("题目")
            .AddText(CurrentWord.firstLine)
            .AddButton(new ToastButton()
                .SetContent("显示答案")
                .AddArgument("action", "showAnswer")
                .SetBackgroundActivation())
            .Show();
        }

        /// <summary>
        /// 闪卡模式 - 显示答案
        /// </summary>
        public void PushFlashCardAnswer(CustomizeWord CurrentWord)
        {
            string answer = CurrentWord.thirdLine;
            if (string.IsNullOrEmpty(answer))
                answer = CurrentWord.secondLine;
            if (string.IsNullOrEmpty(answer))
                answer = CurrentWord.fourthLine;

            new ToastContentBuilder()
            .AddText("答案")
            .AddText(answer)
            .AddButton(new ToastButton()
                .SetContent("认识（移除）")
                .AddArgument("action", "succeed")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("不认识（保留）")
                .AddArgument("action", "fail")
                .SetBackgroundActivation())
            .Show();
        }

        /// <summary>
        /// 闪卡测试模式入口
        /// </summary>
        /// <param name="Num">测试题目数量</param>
        public void FlashCardTest(Object Num)
        {
            int Number = (int)Num;
            Select Query = new Select();
            
            List<CustomizeWord> TestList = Query.GetRandomCustomWords(Number);

            if (TestList == null || TestList.Count == 0)
            {
                PushMessage("请先导入自定义词库！");
                return;
            }

            CustomizeWord CurrentWord = new CustomizeWord();

            while (TestList.Count != 0)
            {
                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(500);
                
                CurrentWord = TestList[0];
                
                // 显示题目
                PushFlashCardQuestion(CurrentWord);
                
                // 等待用户点击"显示答案"
                WORD_CURRENT_STATUS = 2;
                while (WORD_CURRENT_STATUS == 2)
                {
                    var task = ProcessToastNotificationRecitation();
                    if (task.Result == 1 || task.Result == 0)
                    {
                        WORD_CURRENT_STATUS = task.Result;
                    }
                }

                // 显示答案
                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(300);
                PushFlashCardAnswer(CurrentWord);

                // 等待用户判断是否认识
                WORD_CURRENT_STATUS = 2;
                while (WORD_CURRENT_STATUS == 2)
                {
                    var task = ProcessToastNotificationRecitation();
                    if (task.Result == 1)  // 认识
                    {
                        WORD_CURRENT_STATUS = 1;
                    }
                    else if (task.Result == 0)  // 不认识
                    {
                        WORD_CURRENT_STATUS = 0;
                    }
                }

                TestList.Remove(CurrentWord);
                if (WORD_CURRENT_STATUS == 0)  // 不认识，保留到列表末尾
                {
                    TestList.Add(CurrentWord);
                }
            }

            ToastNotificationManagerCompat.History.Clear();
            PushMessage("闪卡测试结束！");
        }

        #endregion
    }
}
