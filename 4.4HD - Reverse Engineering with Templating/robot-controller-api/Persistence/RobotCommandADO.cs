using System;
using System.Collections.Generic;
using Npgsql;

namespace robot_controller_api.Persistence
{
    // ADO.NET implementation of IRobotCommandDataAccess.
    // This class performs direct SQL operations using Npgsql and maps results manually.
    public class RobotCommandADO : IRobotCommandDataAccess
    {
        // Connection string read from environment variable DB_CONNECTION.
        // Intentionally left blank in source for security; set at runtime for demos.
        private readonly string CONNECTION_STRING =
            Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Username=postgres;Password=;Database=sit331";

        // Retrieve all robot commands from the database.
        // Uses a data reader and manual mapping to RobotCommand instances.
        public List<RobotCommand> GetAll()
        {
            var list = new List<RobotCommand>();

            // Open a new connection for this operation.
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            // Parameterless SELECT; results are read sequentially.
            using var cmd = new NpgsqlCommand(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand ORDER BY id",
                conn);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                // description column may be NULL in DB; handle DBNull.
                string? descr = dr.IsDBNull(2) ? null : dr.GetString(2);

                // Manually construct the RobotCommand from reader columns.
                var rc = new RobotCommand(
                    dr.GetInt32(0),
                    dr.GetString(1),
                    dr.GetBoolean(3),
                    dr.GetDateTime(4),
                    dr.GetDateTime(5),
                    descr
                );

                list.Add(rc);
            }

            return list;
        }

        // Retrieve only commands that are movement commands (ismovecommand = true).
        public List<RobotCommand> GetMoveCommands()
        {
            var list = new List<RobotCommand>();
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand WHERE ismovecommand = true ORDER BY id",
                conn);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                string? descr = dr.IsDBNull(2) ? null : dr.GetString(2);
                var rc = new RobotCommand(
                    dr.GetInt32(0),
                    dr.GetString(1),
                    dr.GetBoolean(3),
                    dr.GetDateTime(4),
                    dr.GetDateTime(5),
                    descr
                );
                list.Add(rc);
            }
            return list;
        }

        // Retrieve a single RobotCommand by id. Returns null if not found.
        public RobotCommand? GetById(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand WHERE id = @id",
                conn);
            cmd.Parameters.AddWithValue("id", id);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            string? descr = dr.IsDBNull(2) ? null : dr.GetString(2);
            return new RobotCommand(
                dr.GetInt32(0),
                dr.GetString(1),
                dr.GetBoolean(3),
                dr.GetDateTime(4),
                dr.GetDateTime(5),
                descr
            );
        }

        // Insert a new RobotCommand and return the created entity (with id).
        // Uses RETURNING id to obtain the new primary key in one round trip.
        public RobotCommand Insert(RobotCommand command)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO public.robotcommand (""Name"", description, ismovecommand, createddate, modifieddate)
                  VALUES (@name, @description, @ismove, @createddate, @modifieddate)
                  RETURNING id",
                conn);

            // Add parameters; pass DBNull.Value for nullable description when necessary.
            cmd.Parameters.AddWithValue("name", command.Name);
            cmd.Parameters.AddWithValue("description", (object)command.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ismove", command.IsMoveCommand);
            cmd.Parameters.AddWithValue("createddate", command.CreatedDate);
            cmd.Parameters.AddWithValue("modifieddate", command.ModifiedDate);

            // ExecuteScalar returns the id from RETURNING.
            var idObj = cmd.ExecuteScalar();
            if (idObj == null || idObj == DBNull.Value)
                throw new InvalidOperationException("Failed to insert robot command: no id returned.");

            var id = Convert.ToInt32(idObj);

            // Return a new RobotCommand instance with the assigned id.
            return new RobotCommand(id, command.Name, command.IsMoveCommand, command.CreatedDate, command.ModifiedDate, command.Description);
        }

        // Update an existing RobotCommand by id. Returns true if a row was affected.
        public bool Update(int id, RobotCommand command)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"UPDATE public.robotcommand
                  SET ""Name"" = @name,
                      description = @description,
                      ismovecommand = @ismove,
                      modifieddate = @modifieddate
                  WHERE id = @id",
                conn);

            // Add parameters; use DBNull.Value for nullable description.
            cmd.Parameters.AddWithValue("name", command.Name);
            cmd.Parameters.AddWithValue("description", (object)command.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ismove", command.IsMoveCommand);
            cmd.Parameters.AddWithValue("modifieddate", command.ModifiedDate);
            cmd.Parameters.AddWithValue("id", id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Delete a RobotCommand by id. Returns true if a row was deleted.
        public bool Delete(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM public.robotcommand WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Check whether a command name already exists in the DB.
        public bool NameExists(string name, int? excludeId = null)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            string sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM public.robotcommand WHERE lower(\"Name\") = lower(@name) AND id <> @id"
                : "SELECT COUNT(1) FROM public.robotcommand WHERE lower(\"Name\") = lower(@name)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", name);
            if (excludeId.HasValue) cmd.Parameters.AddWithValue("id", excludeId.Value);

            var countObj = cmd.ExecuteScalar();
            var count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);

            return count > 0;
        }
    }
}
