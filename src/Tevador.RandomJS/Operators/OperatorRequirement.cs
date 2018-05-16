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

namespace Tevador.RandomJS.Operators
{
    [Flags]
    public enum OperatorRequirement
    {
        None = 0,
        NumericOnly = 1 << 0,
        RhsNonzero = 1 << 1,
        RhsNonnegative = 1 << 2,
        FunctionCall = 1 << 3,
        LimitedPrecision = 1 << 4,
        Prefix = 1 << 5,
        WithoutRhs = 1 << 6
    }
}
