﻿using System;
using System.Linq;
using System.Windows.Forms;
using ExecutionPlanVisualizer.Helpers;
using LINQPad;
using Microsoft.EntityFrameworkCore;
using QueryPlanVisualizer.LinqPad6;

namespace ExecutionPlanVisualizer
{
    public static class QueryPlanVisualizer
    {
        private const string ExecutionPlanPanelTitle = "Query Execution Plan";

        public static IQueryable<T> DumpPlan<T>(this IQueryable<T> queryable, bool dumpData = false)
        {
            try
            {
                DumpPlanInternal(queryable, dumpData, true);
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }

            return queryable;
        }

        private static void DumpPlanInternal<T>(IQueryable<T> queryable, bool dumpData, bool addNewPanel)
        {
            var providerName = Util.CurrentQuery.GetConnectionInfo().DriverData.ElementValue("EFProvider");

            if (providerName == null && Util.CurrentDataContext is DbContext context)
            {
                providerName = context.Database.ProviderName;
            }

            var ormHelper = OrmHelper.Create(queryable, providerName);

            if (ormHelper == null)
            {
                ShowError("The selected database or database driver isn't supported");
                return;
            }

            if (dumpData)
            {
                queryable.Dump();
            }

            var planProcessor = ormHelper.GetPlanProcessor(queryable);
            var databaseProvider = ormHelper.GetDatabaseProvider(queryable);

            if (databaseProvider == null)
            {
                ShowError("Selected database not supported");
                return;
            }

            var rawPlan = databaseProvider.ExtractPlan();

            if (string.IsNullOrEmpty(rawPlan))
            {
                ShowError("Cannot extract query plan");
                return;
            }

            var control = new QueryPlanUserControl
            {
                DatabaseProvider = databaseProvider,
                PlanProcessor = planProcessor
            };

            control.DisplayPlan(rawPlan);
            control.Dump(ExecutionPlanPanelTitle);
        }

        private static void ShowError(string text)
        {
            var control = new Label { Text = text };
            control.Dump(ExecutionPlanPanelTitle);
        }
    }
}
