#region Copyright statement
// --------------------------------------------------------------
// Copyright (C) 1999-2016 Exclaimer Ltd. All Rights Reserved.
// No part of this source file may be copied and/or distributed 
// without the express permission of a director of Exclaimer Ltd
// ---------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeveloperTestInterfaces;

namespace DeveloperTest
{
    public sealed class DeveloperTestImplementation : IDeveloperTest
    {
        private readonly object _padlock = new object();
        private readonly string _filterSymbols = new string(CharExtensions.AcceptedSymbols);

        public void RunQuestionOne(ICharacterReader reader, IOutputResult output)
        {
            var wordCount = new Dictionary<string, int>();

            CalculateWords(reader, wordCount);

            SendWordsOrdered(output, wordCount);
        }

        private void SendWordsOrdered(IOutputResult output, Dictionary<string, int> wordCount)
        {
            lock (_padlock)
            {
                foreach (var item in wordCount.OrderBy(o => o.Key).OrderByDescending(o => o.Value))
                {
                    output.AddResult(item.Key + " - " + item.Value);
                }
            }
        }

        private void CalculateWords(ICharacterReader reader, Dictionary<string, int> wordCount)
        {
            string word = "";
            while (true)
            {
                try
                {
                    char letter = reader.GetNextChar();
                    if ((letter.IsAcceptedSymbol() && !word.Contains(_filterSymbols)) || letter.IsLetter())
                    {
                        word += letter;
                    }
                    else
                    {
                        SetWord(wordCount, word);
                        word = "";
                    }
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }

            SetWord(wordCount, word);
        }

        private void SetWord(Dictionary<string, int> wordCount, string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                line = line.EndsWith(_filterSymbols) && line.Length > 1 ? line.Remove(line.Length-1).ToLower() : line.ToLower();
                lock (_padlock)
                {
                    if (wordCount.ContainsKey(line))
                    {
                        wordCount[line] += 1;
                    }
                    else
                    {
                        wordCount.Add(line, 1);
                    }
                }
            }
        }

        public void RunQuestionTwo(ICharacterReader[] readers, IOutputResult output)
        {
            var wordCount = new Dictionary<string, int>();

            var timer = new Timer(
                    e => SendWordsOrdered(output, wordCount),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10));

            Parallel.ForEach(readers, reader =>
            {
                CalculateWords(reader, wordCount);
            });

            timer.Dispose();

            SendWordsOrdered(output, wordCount);
        }
    }
}