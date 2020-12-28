using System.Collections.Generic;
using System.Linq;
using AdventToolkit;
using AdventToolkit.Extensions;
using AdventToolkit.Utilities;
using RegExtract;

namespace AdventOfCode2020.Puzzles
{
    public class Day19 : Puzzle
    {
        public new string[][] Groups;
        public Dictionary<int, string> Rules = new();

        public Day19()
        {
            Groups = base.Groups.ToArray();
            ReadRules();
            Part = 2;
        }

        public void ReadRules()
        {
            var rules = Groups[0].Extract<(int, string)>(@"(\d+): (.+)");
            foreach (var (rule, spec) in rules)
            {
                Rules[rule] = spec;
            }
        }

        public int SearchThrough(string s, int start, char c)
        {
            var level = 0;
            for (var i = start; i < s.Length; i++)
            {
                if (s[i] == c && level == 0) return i;
                if (s[i] == '(') level++;
                else if (s[i] == ')') level--;
            }
            return -1;
        }

        public string ToRegex(int rule)
        {
            var spec = Rules[rule];
            if (spec.Search('|', out var i))
            {
                return $"({ToRegex(spec[..i])}|{ToRegex(spec[(i + 1)..])})";
            }
            return ToRegex(spec);
        }

        public string ToRegex(string part)
        {
            part = part.Trim();
            if (part.StartsWith('"')) return part[1].ToString();
            var parts = part.Split(' ');
            return string.Concat(parts.Select(int.Parse).Select(ToRegex));
        }

        public int[] ToStates(string s, int[] from, StateMachine<char> machine)
        {
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '(')
                {
                    var end = s.GetEndParen(i);
                    var mid = SearchThrough(s, i + 1, '|');
                    from = ToStates(s[(i + 1)..mid], from, machine)
                        .Concat(ToStates(s[(mid + 1)..end], from, machine))
                        .ToArray();
                    i = end;
                }
                else
                {
                    var next = machine.NewState();
                    foreach (var id in from)
                    {
                        machine[(id, c)].Add(next);
                    }
                    from = new[] {next};
                }
            }
            return from;
        }

        public StateMachine<char> StateMachineFor(int rule)
        {
            var machine = new StateMachine<char>();
            machine.NewState(); // initial state
            var ends = ToStates(ToRegex(rule), new[] {0}, machine);
            machine.AcceptingStates.UnionWith(ends);
            if (machine.IsNfa()) machine = machine.NfaToDfa("ab");
            return machine;
        }

        public override void PartOne()
        {
            var machine = StateMachineFor(0);
            WriteLn(Groups[1].Count(machine.Test));
        }

        public override void PartTwo()
        {
            var rule42 = StateMachineFor(42);
            var rule31 = StateMachineFor(31);

            // Count number of times rule 42 matches followed by number
            // of times rule 31 matches. The string matches if rule 42
            // occurs more than rule 31 and both match at least once
            var count = Groups[1]
                .Where(s =>
                {
                    var (count42, end42) = rule42.Count(s);
                    var (count31, end31) = rule31.Count(s[end42..]);
                    if (end42 + end31 != s.Length) return false;
                    return count31 >= 1 && count42 > count31;
                })
                .Count();
            WriteLn(count);
        }
    }
}