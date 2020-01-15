/*
 * Copyright 2008 ZXing authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using ZXing.SkiaSharp.Common.Test;

namespace ZXing.SkiaSharp.OneD.Test
{
   /// <summary>
   /// <author>Sean Owen</author>
   /// </summary>
   public sealed class Code128BlackBox3TestCase : SkiaBarcodeBlackBoxTestCase
   {
      public Code128BlackBox3TestCase()
         : base("../../../../../test/data/blackbox/code128-3", BarcodeFormat.CODE_128)
      {
         addTest(2, 2, 0.0f);
         addTest(2, 2, 180.0f);
      }
   }
}