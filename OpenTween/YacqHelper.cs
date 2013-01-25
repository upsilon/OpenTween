// OpenTween - Client of Twitter
// Copyright (c) 2012 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
//
// This file is part of OpenTween.
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General public License
// for more details.
//
// You should have received a copy of the GNU General public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace OpenTween
{
    public static class YacqHelper
    {
        public static readonly string DllName = "Yacq.dll";

        public static bool CheckCanUse()
        {
            try
            {
                var func = YacqHelper.ParseLambda<Func<int, int>>("(* x x)", "x").Compile();

                return func(5) == 25;
            }
            catch (FileNotFoundException) { } // 単にYacq.dllが無い場合は無視
            catch (Exception e)
            {
                MyCommon.TraceOut(e, "Yacq.dll Load Error");
            }

            return false;
        }

        internal static Assembly yacqAssembly = null;

        public static Assembly GetAssembly(string basedir = null)
        {
            if (YacqHelper.yacqAssembly == null)
            {
                if (basedir == null)
                    basedir = Application.StartupPath;

                var path = Path.Combine(basedir, YacqHelper.DllName);
                YacqHelper.yacqAssembly = Assembly.LoadFrom(path);
            }

            return YacqHelper.yacqAssembly;
        }

        public static Expression<TDelegate> ParseLambda<TDelegate>(string code, params String[] parameterNames)
        {
            var assembly = YacqHelper.GetAssembly();
            var type = assembly.GetType("XSpect.Yacq.YacqServices");

            MethodInfo method = null;
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.IsGenericMethod && m.Name == "ParseLambda")
                {
                    var parameters = m.GetParameters();

                    if (parameters.Length != 2) continue;

                    // Yacq 1.11 まで
                    if (parameters[0].ParameterType == typeof(String) &&
                        parameters[1].ParameterType == typeof(String[]))
                    {
                        method = m.MakeGenericMethod(typeof(TDelegate));
                        break;
                    }

                    // 参照: https://github.com/takeshik/yacq/commit/b30516571bdbb057ecd63649b2e0d039482c3624
                    if (parameters[0].ParameterType == typeof(IEnumerable<Char>) &&
                        parameters[1].ParameterType == typeof(String[]))
                    {
                        method = m.MakeGenericMethod(typeof(TDelegate));
                        break;
                    }
                }
            }

            return (Expression<TDelegate>)method.Invoke(null, new object[] { code, parameterNames });
        }
    }
}
