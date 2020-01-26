using Calculator.Common;
using System.Collections.Generic;

namespace Calculator.Core.Interfaces
{
    public interface ICalculatorService
    {
        List<int> ParseValidNumbersFromInput(string input, CmdOptions options = null);
        int PerformOperationOnNumbers(List<int> numbers, char operation = '+');

    }
}
