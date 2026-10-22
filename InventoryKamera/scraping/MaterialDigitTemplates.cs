using System;
using System.Linq;
namespace InventoryKamera
{
    // Derived average silhouettes of the ten Genshin quantity digits, with no
    // item names, counts, or individual screenshot images retained.
    // Quantized 8x14 pixels per glyph. EXP-017 corpus only.
    internal static class MaterialDigitTemplates
    {
        internal static readonly byte[][] All = new string[]
        {
            "AAiY+PiWBwAA4//f4f/qADD/9wID/f8ju/+fAACy/7Xl/2gAAG//4fz/VwAAYv/5//9PAABP//7//08AAE////z/WQAAXf/55/9sAABu/9qz/6kAALr/sCb//QMI/f8tANL/4eb/uQAABojv7IwGAA==", // 0
            "AAABCzx10O8VLqLj//////v9////////KjWc/P////8AABO1/////wAAEK7/////AAAQrv////8AABCu/////wAAEK7/////AAAQrv////8AABCu/////wAAEK7////8AAAQrv///6MAAAmh7aJSBA==", // 1
            "ACWt9/vJPgAO7v/o6v/+IpX/1AID9/+szP9tAACR/+jq/zQAAJb/6DxcBQAC+f+4AAAAAHv//j8AAACE//19AAACuv/5OAAAAKX//yAAAABH/v9nAAAAAL3//3JeXl/D9v////////Xn////////wg==", // 2
            "AGDh//euKQBh//7h+P/1Ctn/XgIN//947/8IAADl/7NjawAAAfn/jgAAFyyP//osAADR////sAAAAEZ+u//8YQAAAAAB3P/g1dEAAACB///6/wQAAKT/9OH/lwYp///CTv//9v7/9iIAXbr96qArAA==", // 3
            "AAAABvL/AQAAAAB5//8CAAAAAPv//wIAAABC8P//AgAAAPdc//8CAAA4/QD//wIAANSoAP//AgAL/wMA//8CAL3+urr//8aw/f////////+NoKCg//+wkQAAAAD//wIAAAAAAP//AAAAAAAA+VYAAA==", // 4
            "AKr////9/HEA5P/////8LQD6/JOQgBoAAf7uAAAAAAAm/78DCAAAAG7/+fv+uTEAhP/w3/r//R8RLwYAFP3/rAAAAAAA2f/yr5MAAADH///9/gAAANz/7eb/HQMN/v+qYf/45vn/9xEAacz/76AkAA==", // 5
            "AAAAJKvOAAAAACH79hwAAAAC+/8vAAAAANj/mwAAAAAx//4LCQAAAKn//Ln8yU0A6f//4ub//zT3/7sCA+P/xP//OwAAhf/3+v8yAABa///r/4YAAIL/9J//4QIC3/+7Df3/4d7//hgALrT4+rY6AA==", // 6
            "xv/////////t////////uK5APj4+7/8gAAAAACX94wEAAAAA0v9OAAAAACP//wMAAAAAuv/gAAAAAAT8/3cAAAAAEv//HgAAAACC//8EAAAAAMT/+QEAAAAA6v/MAAAAAAD8/4wAAAAAAPG+CwAAAA==", // 7
            "ADnM/vnFNQAH///b3///CI7/4QEC2/9ms//BAADJ/7KU//gdBuj/XB7///nC//IEAJ7/////YQAO9P/8///9K7v/1QUt8//L9/9SAABp//n+/y0AAEL/+dz/hAEBe//TS//+yLv9/0UAVbL38rNJAA==", // 8
            "AEXG+/qoIgAe/v/f4//2CMb/2wEC7v+g+P9tAACM/9b//zoAAHD/+vz/eQAAev/+x//nBAS8/+8y/v/x8f//xwA2z+6j+P97AAAAAB79/BwAAAAAtv++AAAAADb/+gAAAAA4+PMcAAAAAO2fHQAAAA==" // 9
        }.Select(Convert.FromBase64String).ToArray();
    }
}
