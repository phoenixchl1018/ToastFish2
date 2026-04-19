// ================================================================================
// 文件: Model/SqliteControl/Select.cs
// 说明: 在 Select 类中添加以下方法（添加到 #region 英语部分 的末尾，GetRandomWords 方法之后）
// ================================================================================

        #region 自定义词库测试相关方法

        /// <summary>
        /// 存储当前导入的自定义词库列表（用于测试）
        /// </summary>
        public static List<CustomizeWord> CustomWordListForTest = null;

        /// <summary>
        /// 从自定义词库中随机抽取指定数量的单词
        /// </summary>
        /// <param name="Number">抽取数量</param>
        /// <returns>随机抽取的自定义单词列表</returns>
        public List<CustomizeWord> GetRandomCustomWords(int Number)
        {
            List<CustomizeWord> Result = new List<CustomizeWord>();
            
            if (CustomWordListForTest == null || CustomWordListForTest.Count == 0)
            {
                return Result;
            }

            List<CustomizeWord> TempList = new List<CustomizeWord>(CustomWordListForTest);
            Random Rd = new Random();
            
            int actualNumber = Math.Min(Number, TempList.Count);
            
            for (int i = 0; i < actualNumber; i++)
            {
                int Index = Rd.Next(TempList.Count);
                Result.Add(TempList[Index]);
                TempList.RemoveAt(Index);
            }
            return Result;
        }

        /// <summary>
        /// 获取自定义词库的总数量
        /// </summary>
        /// <returns>自定义词库总数量</returns>
        public int GetCustomWordCount()
        {
            if (CustomWordListForTest == null)
                return 0;
            return CustomWordListForTest.Count;
        }

        /// <summary>
        /// 设置自定义词库列表（导入时调用）
        /// </summary>
        /// <param name="wordList">自定义单词列表</param>
        public static void SetCustomWordList(List<CustomizeWord> wordList)
        {
            CustomWordListForTest = wordList;
        }

        /// <summary>
        /// 清空自定义词库列表
        /// </summary>
        public static void ClearCustomWordList()
        {
            CustomWordListForTest = null;
        }

        #endregion
