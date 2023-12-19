using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private const string ApiEndpoint = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();

        }

        private async Task LoadData()
        {


            try
            {
                using (HttpClient client = new HttpClient())
                {


                    HttpResponseMessage response = await client.GetAsync(ApiEndpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        List<TimeEntry> timeEntries = JsonConvert.DeserializeObject<List<TimeEntry>>(json);

                        List<Employee> employees = new List<Employee>();
                        Chart chart = new Chart();
                        ChartArea chartArea = new ChartArea();
                        chart.ChartAreas.Add(chartArea);
                        Series series = new Series();
                        chart.Series.Add(series);
                        foreach (TimeEntry timeEntry in timeEntries)
                        {
                            Employee employee = new Employee();
                            var workingHours = timeEntry.EndTimeUtc - timeEntry.StarTimeUtc;
                            employee.Name = timeEntry.EmployeeName;
                            employee.TotalTimeWorked = workingHours.Hours;
                            employees.Add(employee);
                        }

                        var lnq = from emp in employees
                                  group emp by emp.Name into temp
                                  select new Employee
                                  {
                                      Name = temp.DistinctBy(e => e.Name).Select(e => e.Name).FirstOrDefault(),
                                      TotalTimeWorked = temp.Sum(e => e.TotalTimeWorked)
                                  };

                        employees = lnq.ToList();



                        // Sort employees by total time worked
                        employees.Sort((emp1, emp2) => emp1.TotalTimeWorked.CompareTo(emp2.TotalTimeWorked));

                        // Display data in HTML table
                        string htmlTable = "<html><head><style>table { border-collapse: collapse; width: 100%; }" +
                                           "th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }" +
                                           "th { background-color: #f2f2f2; }</style></head><body><table>" +
                                           "<tr><th>Name</th><th>Total Time Worked</th></tr>";

                        foreach (Employee employee in employees)
                        {
                            string rowColor = (employee.TotalTimeWorked < 100) ? "style='background-color: #ffcccc;'" : "";
                            htmlTable += $"<tr {rowColor}><td>{employee.Name}</td><td>{employee.TotalTimeWorked}</td></tr>";
                            DataPoint point = new DataPoint();
                            point.SetValueXY(String.IsNullOrEmpty(employee.Name) ? "UnKnown" + " " + (employees.Sum(e => e.TotalTimeWorked) / employee.TotalTimeWorked) + "%" : employee.Name + " " + (employees.Sum(e => e.TotalTimeWorked) / employee.TotalTimeWorked) + "%", employee.TotalTimeWorked);
                            series.Points.Add(point);
                        }
                        series.ChartType = SeriesChartType.Pie;

                        // Save the chart as a PNG file
                        string outputPath = Path.Combine("C:\\Users\\palle\\Desktop\\piechart", "pie_chart.png");

                        chart.SaveImage(outputPath, ChartImageFormat.Png);

                        htmlTable += "</table></body></html>";

                        webBrowser1.DocumentText = htmlTable;
                    }
                    else
                    {
                        MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
    }

    public class Employee
    {
        public string Name { get; set; }
        public int TotalTimeWorked { get; set; }
    }

    public class TimeEntry
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string EntryNotes { get; set; }
        public DateTime? DeletedOn { get; set; }
    }

}
