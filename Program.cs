using System.Configuration;
using Microsoft.Data.SqlClient;
using QuteApp.Forms;

namespace QuteApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

       