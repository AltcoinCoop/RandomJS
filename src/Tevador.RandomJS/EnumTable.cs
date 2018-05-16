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
using System.Xml;

namespace Tevador.RandomJS
{
    public class EnumTable<T> : RandomTable<T>
        where T : struct
    {
        public EnumTable()
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("Invalid generic argument type, expected Enum");
        }

        protected override void AddValue(string valueStr, double weight, XmlReader reader)
        {
            T type;
            if (!Enum.TryParse(valueStr, out type))
                Error("Invalid value of attribute 'type' = " + valueStr, reader);

            Add(type, weight);
        }
    }
}
