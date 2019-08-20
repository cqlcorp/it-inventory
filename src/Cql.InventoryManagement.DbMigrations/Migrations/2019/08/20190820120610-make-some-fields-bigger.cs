using System;
using System.Collections.Generic;
using System.Text;
using FluentMigrator;

namespace Cql.InventoryManagement.DbMigrations.Migrations._2019._07
{
    [Migration(20190820120610)]
    public class _20190820120610_make_some_fields_bigger : AutoReversingMigration
    {
        public override void Up()
        {
            Alter.Table("Employee")
                .AlterColumn("Role")
                .AsString(100)
                .NotNullable();

            Alter.Table("Employee")
                .AlterColumn("TextField")
                .AsString(500)
                .Nullable();

            Alter.Table("Server")
                .AlterColumn("TextField")
                .AsString(500)
                .Nullable();

            Alter.Table("Computer")
                .AlterColumn("TextField")
                .AsString(500)
                .Nullable();

            Alter.Table("Monitor")
                .AlterColumn("TextField")
                .AsString(500)
                .Nullable();

            Alter.Table("Peripheral")
                .AlterColumn("TextField")
                .AsString(500)
                .Nullable();

        }
    }
}
