using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum FeePlan { Hourly, Flat, Progressive }

public class ParkingSession
{
    public DateTime EntryTime { get; }
    public DateTime ExitTime { get; }
    public TimeSpan Duration { get; }
    public int RoundedHours { get; }

    public ParkingSession(DateTime entryTime, DateTime exitTime)
    {
        if (exitTime <= entryTime)
            throw new ArgumentException("Exit time must be after entry time");

        EntryTime = entryTime;
        ExitTime = exitTime;
        Duration = exitTime - entryTime;
        RoundedHours = (int)Math.Ceiling(Duration.TotalHours);
    }

    public decimal CalculateFee(FeePlan plan, FeeSchedule schedule)
    {
        Console.WriteLine($"Calculating fee for plan: {plan}, RoundedHours: {RoundedHours}");

        switch (plan)
        {
            case FeePlan.Hourly:
                decimal hourlyFee = schedule.HourlyRate * RoundedHours;
                Console.WriteLine($"Hourly Fee: {hourlyFee}");
                return hourlyFee;

            case FeePlan.Flat:
                decimal flatFee;
                if (RoundedHours <= schedule.FlatRateHours)
                {
                    flatFee = schedule.FlatRate;
                }
                else
                {
                    flatFee = schedule.FlatRate + (RoundedHours - schedule.FlatRateHours) * schedule.ExcessRate;
                }
                Console.WriteLine($"Flat Fee: {flatFee}");
                return flatFee;

            case FeePlan.Progressive:
                decimal progressiveFee = CalculateProgressiveFee(schedule);
                Console.WriteLine($"Progressive Fee: {progressiveFee}");
                return progressiveFee;

            default:
                throw new ArgumentException("Invalid fee plan");
        }
    }


    private decimal CalculateProgressiveFee(FeeSchedule schedule)
    {
        decimal total = 0;
        int remainingHours = RoundedHours;
        int tier = 0;
        while (remainingHours > 0 && tier < schedule.ProgressiveRates.Count)
        {
            int hoursInTier = Math.Min(remainingHours, schedule.ProgressiveTiers[tier]);
            total += hoursInTier * schedule.ProgressiveRates[tier];
            remainingHours -= hoursInTier;
            tier++;
        }

        if (remainingHours > 0 && tier < schedule.ProgressiveRates.Count)
        {
            total += remainingHours * schedule.ProgressiveRates.Last();
        }
        return total;
    }

    public (FeePlan plan, decimal amount) GetBestPlan(FeeSchedule schedule)
    {
        var options = new Dictionary<FeePlan, decimal>
        {
            [FeePlan.Hourly] = CalculateFee(FeePlan.Hourly, schedule),
            [FeePlan.Flat] = CalculateFee(FeePlan.Flat, schedule),
            [FeePlan.Progressive] = CalculateFee(FeePlan.Progressive, schedule)
        };
        var bestOption = options.OrderBy(kv => kv.Value).First();
        return (bestOption.Key, bestOption.Value);
    }
}

public class FeeSchedule
{
    public decimal HourlyRate { get; set; }
    public decimal FlatRate { get; set; }
    public int FlatRateHours { get; set; }
    public decimal ExcessRate { get; set; }
    public List<int> ProgressiveTiers { get; set; }
    public List<decimal> ProgressiveRates { get; set; }
    public FeeSchedule()
    {
        ProgressiveTiers = new List<int>();
        ProgressiveRates = new List<decimal>();
    }
}
public class ParkingCalculator
{
    public static void ProcessSessions(List<ParkingSession> sessions, FeeSchedule schedule)
    {
        Console.WriteLine("\nSession Results:");
        Console.WriteLine(new string('-', 85));
        Console.WriteLine($"{"Session",-8} {"Hours",-6} {"Hourly",-10} {"Flat",-10} {"Progressive",-15} {"Best Plan",-15} Amount");
        Console.WriteLine(new string('-', 85));
        foreach (var session in sessions)
        {
            var hourly = session.CalculateFee(FeePlan.Hourly, schedule);
            var flat = session.CalculateFee(FeePlan.Flat, schedule);
            var progressive = session.CalculateFee(FeePlan.Progressive, schedule);
            var best = session.GetBestPlan(schedule);
            Console.WriteLine($"{sessions.IndexOf(session) + 1,-8} {session.RoundedHours,-6} " +
                            $"${hourly,-9:F2} ${flat,-9:F2} ${progressive,-14:F2} " +
                            $"{best.plan,-15} ${best.amount:F2}");
        }
    }
}

namespace Parking_Lot_Fee_Optimizer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var schedule = new FeeSchedule
            {
                HourlyRate = 3.50m,
                FlatRate = 10.00m,
                FlatRateHours = 4,
                ExcessRate = 2.50m,
                ProgressiveTiers = new List<int> { 2, 3, 0 },
                ProgressiveRates = new List<decimal> { 4.00m, 3.00m, 2.00m }
            };
            Console.WriteLine("Parking Fee Comparison Tool\n");
            Console.WriteLine("Fee Schedule:");
            Console.WriteLine($"- Hourly Rate: ${schedule.HourlyRate}/hr");
            Console.WriteLine($"- Flat Rate: ${schedule.FlatRate} for first {schedule.FlatRateHours} hrs, then ${schedule.ExcessRate}/hr");
            Console.WriteLine($"- Progressive: ${schedule.ProgressiveRates[0]} for first {schedule.ProgressiveTiers[0]} hrs, " +
                            $"${schedule.ProgressiveRates[1]} for next {schedule.ProgressiveTiers[1]} hrs, " +
                            $"${schedule.ProgressiveRates[2]}/hr thereafter\n");
            Console.WriteLine("Enter parking sessions (format: MM/dd/yyyy HH:mm - MM/dd/yyyy HH:mm) one per line, empty to finish:");
            var sessions = new List<ParkingSession>();

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;

                try
                {
                    var parts = input.Split('-');
                    var entry = DateTime.Parse(parts[0].Trim());
                    var exit = DateTime.Parse(parts[1].Trim());
                    sessions.Add(new ParkingSession(entry, exit));
                }
                catch
                {
                    Console.WriteLine("Invalid format. Use: MM/dd/yyyy HH:mm - MM/dd/yyyy HH:mm");
                }
            }

            ParkingCalculator.ProcessSessions(sessions, schedule);
        }
    }
}
