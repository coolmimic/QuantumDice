using System.Text.Json;

namespace QuantumDice.Api.Services;

/// <summary>
/// 开奖结果判断服务
/// </summary>
public interface IBetResultService
{
    bool CheckWin(string betMethodCode, string? parsedContent, int[] diceValues);
}

public class BetResultService : IBetResultService
{
    public bool CheckWin(string betMethodCode, string? parsedContent, int[] diceValues)
    {
        var total = diceValues.Sum();
        var diceCount = diceValues.Length;

        return betMethodCode switch
        {
            // 扫雷 (1骰子)
            "Position" when diceCount == 1 => CheckPosition1D(parsedContent, diceValues),
            
            // 龙虎 (2骰子)
            "Position" when diceCount == 2 => CheckPosition2D(parsedContent, diceValues),
            "Dragon" when diceCount == 2 => diceValues[0] > diceValues[1],
            "Tiger" when diceCount == 2 => diceValues[0] < diceValues[1],
            "Tie" when diceCount == 2 => diceValues[0] == diceValues[1],

            // 快三 (3骰子)
            "Position" when diceCount == 3 => CheckPosition3D(parsedContent, diceValues),
            "Compound" => CheckCompound(parsedContent, diceValues),
            
            // 大小单双
            "Big" => CheckBig(total, diceCount, diceValues),
            "Small" => CheckSmall(total, diceCount, diceValues),
            "Odd" => CheckOdd(total, diceCount, diceValues),
            "Even" => CheckEven(total, diceCount, diceValues),
            "BigOdd" => CheckBig(total, diceCount, diceValues) && total % 2 == 1,
            "BigEven" => CheckBig(total, diceCount, diceValues) && total % 2 == 0,
            "SmallOdd" => CheckSmall(total, diceCount, diceValues) && total % 2 == 1,
            "SmallEven" => CheckSmall(total, diceCount, diceValues) && total % 2 == 0,

            // 快三龙虎
            "Dragon" when diceCount == 3 => diceValues[0] > diceValues[2], // 默认1比3
            "Tiger" when diceCount == 3 => diceValues[0] < diceValues[2],
            "Tie" when diceCount == 3 => diceValues[0] == diceValues[2],
            "FrontDragon" => diceCount == 3 && diceValues[0] > diceValues[1],
            "BackDragon" => diceCount == 3 && diceValues[1] > diceValues[2],

            // 快三特殊
            "Leopard" => CheckLeopard(parsedContent, diceValues),
            "Straight" => CheckStraight(diceValues),
            "GroupThree" => CheckGroupThree(diceValues),
            "GroupSix" => CheckGroupSix(diceValues),

            _ => false
        };
    }

    // 扫雷定位胆
    private bool CheckPosition1D(string? parsedContent, int[] diceValues)
    {
        if (string.IsNullOrEmpty(parsedContent)) return false;
        try
        {
            var doc = JsonDocument.Parse(parsedContent);
            var numbers = doc.RootElement.GetProperty("numbers").EnumerateArray()
                .Select(e => e.GetInt32()).ToArray();
            return numbers.Contains(diceValues[0]);
        }
        catch { return false; }
    }

    // 龙虎定位胆
    private bool CheckPosition2D(string? parsedContent, int[] diceValues)
    {
        if (string.IsNullOrEmpty(parsedContent)) return false;
        try
        {
            var doc = JsonDocument.Parse(parsedContent);
            var num1 = doc.RootElement.GetProperty("num1").GetInt32();
            var num2 = doc.RootElement.GetProperty("num2").GetInt32();
            return diceValues[0] == num1 && diceValues[1] == num2;
        }
        catch { return false; }
    }

    // 快三定位胆
    private bool CheckPosition3D(string? parsedContent, int[] diceValues)
    {
        if (string.IsNullOrEmpty(parsedContent)) return false;
        try
        {
            var doc = JsonDocument.Parse(parsedContent);
            var numbers = doc.RootElement.GetProperty("numbers").EnumerateArray()
                .Select(e => e.GetInt32()).ToArray();
            
            // 单号投注：任意位置匹配
            if (numbers.Length == 1)
                return diceValues.Contains(numbers[0]);
            
            // 三号投注：完全匹配
            if (numbers.Length == 3)
                return diceValues[0] == numbers[0] && 
                       diceValues[1] == numbers[1] && 
                       diceValues[2] == numbers[2];
            
            return false;
        }
        catch { return false; }
    }

    // 复式
    private bool CheckCompound(string? parsedContent, int[] diceValues)
    {
        if (string.IsNullOrEmpty(parsedContent)) return false;
        try
        {
            var doc = JsonDocument.Parse(parsedContent);
            var numbers = doc.RootElement.GetProperty("numbers").EnumerateArray()
                .Select(e => e.GetInt32()).ToArray();
            
            return diceValues[0] == numbers[0] && 
                   diceValues[1] == numbers[1] && 
                   diceValues[2] == numbers[2];
        }
        catch { return false; }
    }

    // 大 (快三: 11-17, 豹子通杀)
    private bool CheckBig(int total, int diceCount, int[] diceValues)
    {
        if (diceCount == 1) return diceValues[0] >= 4;
        if (diceCount == 3)
        {
            if (IsLeopard(diceValues)) return false; // 豹子通杀
            return total >= 11 && total <= 17;
        }
        return false;
    }

    // 小 (快三: 4-10, 豹子通杀)
    private bool CheckSmall(int total, int diceCount, int[] diceValues)
    {
        if (diceCount == 1) return diceValues[0] <= 3;
        if (diceCount == 3)
        {
            if (IsLeopard(diceValues)) return false;
            return total >= 4 && total <= 10;
        }
        return false;
    }

    // 单
    private bool CheckOdd(int total, int diceCount, int[] diceValues)
    {
        if (diceCount == 1) return diceValues[0] % 2 == 1;
        if (diceCount == 3)
        {
            if (IsLeopard(diceValues)) return false;
            return total % 2 == 1;
        }
        return false;
    }

    // 双
    private bool CheckEven(int total, int diceCount, int[] diceValues)
    {
        if (diceCount == 1) return diceValues[0] % 2 == 0;
        if (diceCount == 3)
        {
            if (IsLeopard(diceValues)) return false;
            return total % 2 == 0;
        }
        return false;
    }

    // 豹子
    private bool CheckLeopard(string? parsedContent, int[] diceValues)
    {
        if (!IsLeopard(diceValues)) return false;
        
        // 如果指定了具体数字
        if (!string.IsNullOrEmpty(parsedContent))
        {
            try
            {
                var doc = JsonDocument.Parse(parsedContent);
                var leopard = doc.RootElement.GetProperty("leopard").GetInt32();
                return diceValues[0] == leopard;
            }
            catch { }
        }
        
        return true; // 通杀型豹子
    }

    private bool IsLeopard(int[] diceValues) => 
        diceValues.Length == 3 && diceValues.Distinct().Count() == 1;

    // 顺子 (支持循环: 123,234,345,456,561,612,012 等)
    private bool CheckStraight(int[] diceValues)
    {
        if (diceValues.Length != 3) return false;
        
        var sorted = diceValues.OrderBy(x => x).ToArray();
        
        // 普通顺子: 123, 234, 345, 456
        if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1)
            return true;
        
        // 循环顺子: 561 (1,5,6), 612 (1,2,6), 012 如果有0则是 1,2,6
        // 在1-6范围内的循环: 5,6,1 和 6,1,2
        var set = new HashSet<int>(diceValues);
        if (set.SetEquals(new[] { 5, 6, 1 })) return true;
        if (set.SetEquals(new[] { 6, 1, 2 })) return true;
        
        return false;
    }

    // 组三 (一对)
    private bool CheckGroupThree(int[] diceValues)
    {
        if (diceValues.Length != 3) return false;
        var distinct = diceValues.Distinct().Count();
        return distinct == 2; // 恰好两个不同的数
    }

    // 组六 (全不同)
    private bool CheckGroupSix(int[] diceValues)
    {
        if (diceValues.Length != 3) return false;
        return diceValues.Distinct().Count() == 3;
    }
}
