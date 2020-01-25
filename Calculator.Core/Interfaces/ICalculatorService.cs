using System;
using System.Collections.Generic;
using System.Text;

namespace Calculator.Core.Interfaces
{
    public interface ICalculatorService
    {
        int[] ParseInput(string input);
        int AddNumbers(int[] numbers);

    }
}
