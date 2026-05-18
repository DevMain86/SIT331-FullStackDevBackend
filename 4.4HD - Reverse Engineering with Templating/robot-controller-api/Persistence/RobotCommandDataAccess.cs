using System;
using System.Collections.Generic;
using Npgsql;
using robot_controller_api;

namespace robot_controller_api.Persistence
{
    // Data access methods for RobotCommand entities.
    // Encapsulates SQL and ADO.NET usage so controllers remain focused on HTTP concerns.
    public static class RobotCommandDataAccess
    {
        // Connection string read from environment or fallback to a local default.
        private static readonly string CONNECTION_STRING =
            Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Username=postgres;Password=;Database=sit331";

        // Retrieve all robot commands ordered by id.
        public static List<RobotCommand> GetAll()
        {
            var list = new List<RobotCommand>();
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand ORDER BY id",
                conn);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                // description may be NULL in the database
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

        // Retrieve only commands that are marked as move commands.
        public static List<RobotCommand> GetMoveCommands()
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

        // Get a single robot command by id. Returns null if not found.
        public static RobotCommand? GetById(int id)
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

        // Insert a new robot command and return the created entity (with id).
        // Throws InvalidOperationException if the DB does not return an id.
        public static RobotCommand Insert(RobotCommand command)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO public.robotcommand (""Name"", description, ismovecommand, createddate, modifieddate)
                  VALUES (@name, @description, @ismove, @createddate, @modifieddate)
                  RETURNING id",
                conn);

            cmd.Parameters.AddWithValue("name", command.Name);
            cmd.Parameters.AddWithValue("description", (object)command.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ismove", command.IsMoveCommand);
            cmd.Parameters.AddWithValue("createddate", command.CreatedDate);
            cmd.Parameters.AddWithValue("modifieddate", command.ModifiedDate);

            try
            {
                var idObj = cmd.ExecuteScalar();
                if (idObj == null || idObj == DBNull.Value)
                    throw new InvalidOperationException("Failed to insert robot command: no id returned.");
                var id = Convert.ToInt32(idObj);

                return new RobotCommand(id, command.Name, command.IsMoveCommand, command.CreatedDate, command.ModifiedDate, command.Description);
            }
            catch (PostgresException)
            {
                // Let higher-level code or middleware handle database-specific exceptions.
                throw;
            }
        }

        // Update an existing robot command. Returns true if a row was affected.
        public static bool Update(int id, RobotCommand command)
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

            cmd.Parameters.AddWithValue("name", command.Name);
            cmd.Parameters.AddWithValue("description", (object)command.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ismove", command.IsMoveCommand);
            cmd.Parameters.AddWithValue("modifieddate", command.ModifiedDate);
            cmd.Parameters.AddWithValue("id", id);

            try
            {
                var affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
            catch (PostgresException)
            {
                // Bubble up DB exceptions for centralized handling.
                throw;
            }
        }

        // Delete a robot command by id. Returns true if a row was deleted.
        public static bool Delete(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM public.robotcommand WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Check whether a command name exists (case-insensitive).
        // Optionally exclude a specific id (useful when updating).
        public static bool NameExists(string name, int? excludeId = null)
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
