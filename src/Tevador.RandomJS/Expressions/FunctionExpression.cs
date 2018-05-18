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

using System.Collections.Generic;
using Tevador.RandomJS.Statements;

namespace Tevador.RandomJS.Expressions
{
    class FunctionExpression : Expression, IScope
    {
        public FunctionExpression(IScope parentScope, Expression parentExpression)
            : base(parentExpression)
        {
            Parent = parentScope;
            VariableCounter = Parent.VariableCounter;
            StatementDepth = Parent.StatementDepth; //increased in Body
        }

        public readonly List<Variable> Parameters = new List<Variable>();
        private List<Variable> _unusedVariables = new List<Variable>(); // TODO
        public Expression DefaultReturnValue { get; set; }
        public Block Body { get; set; }

        public IEnumerable<Variable> Variables
        {
            get
            {
                if (Parent != null)
                {
                    foreach (var v in Parent.Variables)
                    {
                        yield return v;
                    }
                }
                foreach (var v in Parameters)
                {
                    yield return v;
                }
            }
        }

        public IScope Parent
        {
            get;
            private set;
        }

        public override void WriteTo(System.IO.TextWriter w)
        {
            w.Write("function (");
            using (var enumerator = Parameters.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    bool isLast;
                    do
                    {
                        var param = enumerator.Current;
                        isLast = !enumerator.MoveNext();

                        w.Write(param.Name);
                        if (!isLast)
                            w.Write(", ");
                    }
                    while (!isLast);
                }
            }
            w.Write(")");
            Body.WriteTo(w);
        }


        public int VariableCounter
        {
            get;
            set;
        }

        public void Require(Global gf)
        {
            Parent.Require(gf);
        }

        public bool InFunc
        {
            get { return true; }
        }

        public bool HasBreak
        {
            get { return false; }
        }
    }
}
