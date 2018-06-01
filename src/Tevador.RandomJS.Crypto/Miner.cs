﻿/*
    (c) 2018 tevador <tevador@gmail.com>

    This file is part of Tevador.RandomJS.

    Tevador.RandomJS is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Tevador.RandomJS is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Tevador.RandomJS.  If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Text;
using Tevador.RandomJS.Crypto.Blake;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;

namespace Tevador.RandomJS.Crypto
{
    class Miner
    {
        const int N = 8; //asymmetry: solving requires 2^N times more effort than verifying
        const int _bound = (1 << (8 - N));
        const byte _clearMask = (_bound - 1);
        const int _nonceOffset = 39;
        Blake2B256 _blake = new Blake2B256();
        Blake2B256 _blakeKeyed;
        ProgramFactory _factory = new ProgramFactory();
        byte[] _blockTemplate;
        ProgramRunner _runner = new ProgramRunner();

        public void Reset(byte[] blockTemplate)
        {
            _blockTemplate = blockTemplate;
        }

        public unsafe Solution Solve()
        {
            byte[] result = null;
            byte[] auxiliary = null;
            uint nonce;
            fixed (byte* block = _blockTemplate)
            {
                Console.WriteLine(BinaryUtils.ByteArrayToString(_blake.ComputeHash(_blockTemplate)));
                uint* noncePtr = (uint*)(block + _nonceOffset);
                do
                {
                    (*noncePtr)++;
                    byte[] key = _blake.ComputeHash(_blockTemplate);
                    var program = _factory.GenProgram(key);
                    _runner.WriteProgram(program);
                    _blakeKeyed = new Blake2B256(key);
                    auxiliary = _blakeKeyed.ComputeHash(_runner.Buffer, 0, _runner.ProgramLength);
                    var ri = _runner.ExecuteProgram();
                    if(!ri.Success)
                    {
                        throw new Exception(string.Format($"Program execution failed. Nonce value: {(*noncePtr)}. Seed: {BinaryUtils.ByteArrayToString(key)}, {ri.Output}"));
                    }
                    result = _blakeKeyed.ComputeHash(Encoding.ASCII.GetBytes(ri.Output));
                }
                while ((result[0] ^ auxiliary[0]) >= _bound);
                Console.WriteLine("A={0}", BinaryUtils.ByteArrayToString(auxiliary));
                Console.WriteLine("B={0}", BinaryUtils.ByteArrayToString(result));
                nonce = *noncePtr;
            }
            result[0] &= _clearMask;
            for(int i = 0; i < result.Length; ++i)
            {
                result[i] ^= auxiliary[i];
            }
            Console.WriteLine("R={0}", BinaryUtils.ByteArrayToString(result));
            return new Solution()
            {
                Nonce = nonce,
                Result = result,
                ProofOfWork = _blakeKeyed.ComputeHash(result)
            };
        }

        public bool Verify(Solution sol)
        {
            for (int i = 0; i < 4; ++i)
            {
                _blockTemplate[_nonceOffset + i] = (byte)(sol.Nonce >> (8 * i));
            }
            byte[] key = _blake.ComputeHash(_blockTemplate);
            _blakeKeyed = new Blake2B256(key);
            var pow = _blakeKeyed.ComputeHash(sol.Result);
            if(!BinaryUtils.ArraysEqual(pow, sol.ProofOfWork))
            {
                Console.WriteLine("Invalid PoW");
                return false;
            }
            var program = _factory.GenProgram(key);
            _runner.WriteProgram(program);
            var auxiliary = _blakeKeyed.ComputeHash(_runner.Buffer, 0, _runner.ProgramLength);
            if ((auxiliary[0] ^ sol.Result[0]) >= _bound)
            {
                Console.WriteLine("Invalid Auxiliary");
                return false;
            }
            auxiliary[0] &= _clearMask;
            var ri = _runner.ExecuteProgram();
            if (!ri.Success)
            {
                throw new Exception(string.Format($"Program execution failed. Nonce value: {(sol.Nonce)}. Seed: {BinaryUtils.ByteArrayToString(key)}, {ri.Output}"));
            }
            var result = _blakeKeyed.ComputeHash(Encoding.ASCII.GetBytes(ri.Output));
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] ^= auxiliary[i];
            }
            if (!BinaryUtils.ArraysEqual(sol.Result, result))
            {
                Console.WriteLine("Invalid Result");
                return false;
            }
            return true;
        }

        private void MakeStats(int count)
        {
            var runtimes = new List<RuntimeInfo>(count);
            var factory = new ProgramFactory();
            IProgram p;
            RuntimeInfo ri;
            EntropyCounter ec = new EntropyCounter();
            //warm up
            p = factory.GenProgram(BinaryUtils.GenerateSeed(Environment.TickCount));
            _runner.WriteProgram(p);
            ri = _runner.ExecuteProgram();
            Console.WriteLine($"Collecting statistics from {count} random program executions");
            double step = count / 20.0;
            double next = step;
            double dcount = count;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; ++i)
            {
                var seed = Environment.TickCount + i;
                var gs = BinaryUtils.GenerateSeed(seed);
                var ss = BinaryUtils.ByteArrayToString(gs);
                if (i >= next)
                {
                    Console.Write($"{i / dcount:P0}, ");
                    next += step;
                }
                p = factory.GenProgram(gs);
                _runner.WriteProgram(p);
                ri = _runner.ExecuteProgram();
                if (!ri.Success)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error Seed = {0}", ss);
                    Console.WriteLine(ri.Output);
                    break;
                }
                ri.Seed = ss;
                runtimes.Add(ri);
                ec.Add(ri.Output);
            }
            sw.Stop();
            Console.WriteLine();

            Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds} seconds");

            if (!ri.Success) return;

            runtimes.Sort();
            Console.WriteLine($"Longest runtimes:");
            for (int i = 1; i <= 10; ++i)
            {
                var r = runtimes[runtimes.Count - i];
                Console.WriteLine($"Seed = {r.Seed}, Runtime = {r.Runtime:0.00000} s");
            }
            var runtimeStats = new ListStats<RuntimeInfo>(runtimes, r => r.Runtime);
            Console.WriteLine($"Runtime [s] Min: {runtimeStats.Min:0.00000}; Max: {runtimeStats.Max:0.00000}; Avg: {runtimeStats.Average:0.00000}; Stdev: {runtimeStats.StdDev:0.00000};");
            Console.WriteLine($"Runtime [s] 99.99th percentile: {runtimeStats.GetPercentile(0.9999)}");
            Console.WriteLine($"Average entropy of program output (est.): {ec.GetEntropy()} bits");
            var ccStats = new ListStats<RuntimeInfo>(runtimes, r => r.CyclomaticComplexity);
            Console.WriteLine($"Cyclomatic complexity Min: {ccStats.Min}; Max: {ccStats.Max}; Avg: {ccStats.Average}; Stdev: {ccStats.StdDev};");
            var hdStats = new ListStats<RuntimeInfo>(runtimes, r => r.HalsteadDifficulty);
            Console.WriteLine($"Halstead difficulty Min: {hdStats.Min}; Max: {hdStats.Max}; Avg: {hdStats.Average}; Stdev: {hdStats.StdDev};");
            int[] histogram = new int[(int)Math.Ceiling((runtimeStats.Max - runtimeStats.Min) / runtimeStats.StdDev * 10)];
            foreach (var run in runtimes)
            {
                var index = (int)(((run.Runtime - runtimeStats.Min) / runtimeStats.StdDev * 10));
                histogram[index]++;
            }
            Console.WriteLine("Runtime histogram:");
            for (int j = 0; j < histogram.Length; ++j)
            {
                Console.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00000} {1}", j * runtimeStats.StdDev / 10 + runtimeStats.Min, histogram[j]));
            }
        }

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--stats")
            {
                var miner = new Miner();
                int count;
                if (args.Length == 1 || !int.TryParse(args[1], out count))
                    count = 10000;
                miner.MakeStats(count);
                return;
            }
            string blockTemplateHex = "0707f7a4f0d605b303260816ba3f10902e1a145ac5fad3aa3af6ea44c11869dc4f853f002b2eea0000000077b206a02ca5b1d4ce6bbfdf0acac38bded34d2dcdeef95cd20cefc12f61d56109";
            if (args.Length > 0)
            {
                blockTemplateHex = args[0];
            }
            if (blockTemplateHex.Length != 152 || blockTemplateHex.Any(c => !"0123456789abcdef".Contains(c)))
            {
                Console.WriteLine("Invalid block template (152 hex characters expected).");
            }
            else
            {
                try
                {
                    var blockTemplate = BinaryUtils.StringToByteArray(blockTemplateHex);
                    var miner = new Miner();
                    miner.Reset(blockTemplate);
                    TimeSpan period = TimeSpan.FromMinutes(1);
                    List<Solution> solutions = new List<Solution>(100);
                    Stopwatch sw = Stopwatch.StartNew();
                    while (sw.Elapsed < period)
                    {
                        var solution = miner.Solve();
                        Console.WriteLine($"Nonce = {solution.Nonce}; PoW = {BinaryUtils.ByteArrayToString(solution.ProofOfWork)}");
                        solutions.Add(solution);
                    }
                    sw.Stop();
                    var seconds = sw.Elapsed.TotalSeconds;
                    Console.WriteLine();
                    Console.WriteLine($"Solving nonces: {string.Join(", ", solutions.Select(s => s.Nonce))}");
                    Console.WriteLine();
                    Console.WriteLine($"Found {solutions.Count} solutions in {seconds} seconds. Performance = {solutions.Count / seconds} Sols./s.");
                    sw.Restart();
                    foreach (var sol in solutions)
                    {
                        if (!miner.Verify(sol))
                        {
                            Console.WriteLine($"Nonce {sol.Nonce} - verification failed");
                            return;
                        }
                    }
                    sw.Stop();
                    Console.WriteLine($"All {solutions.Count} solutions were verified in {sw.Elapsed.TotalSeconds} seconds");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
            }
        }
    }
}
