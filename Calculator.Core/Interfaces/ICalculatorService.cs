using System;
using System.Collections.Generic;
using System.Text;

namespace Calculator.Core.Interfaces
{
    public interface ICalculatorService
    {
        List<int> ParseInput(string input);
        int AddNumbers(List<int> numbers);

    }
}
