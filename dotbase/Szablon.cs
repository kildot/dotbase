﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.Data;
using DotBase.Logging;

namespace DotBase
{
    partial class Szablon
    {

        private enum TypPolecenia
        {
            NONE,
            UPDATE,
            INSERT,
            DELETE,
        }

        private struct ValueWithOleDbType
        {
            public object value;
            public OleDbType oleDbType;
            public ValueWithOleDbType(object value, OleDbType oleDbType)
            {
                this.value = value;
                this.oleDbType = oleDbType;
            }
        }

        public class Tabela
        {
            private BazaDanychWrapper baza;
            private string nazwa;
            private TypPolecenia typPolecenia = TypPolecenia.NONE;
            private bool where = false;
            private List<ValueWithOleDbType> parameters = new List<ValueWithOleDbType>();
            private string query = "";
            private string selectQuery = "";
            private string values = "";
            private string logParameters = "";
            private string logInfo = "";
            private bool firstEntry = true;
            private int affectedRows;
            private long lastId;
            private bool lastIdValid = false;

            public Tabela(BazaDanychWrapper baza, string nazwa)
            {
                this.baza = baza;
                this.nazwa = nazwa;
            }

            protected void SetField(string name, object value, OleDbType oleDbType)
            {
                if (where)
                {
                    if (!firstEntry)
                    {
                        query += " AND ";
                        selectQuery += " AND ";
                        logParameters += ",\r\n";
                    }
                    firstEntry = false;
                    query += "(" + name + "=?)";
                    selectQuery += "(" + name + "=?)";
                    logParameters += "WHERE " + name + "=" + toLogString(value);
                    parameters.Add(new ValueWithOleDbType(value, oleDbType));
                }
                else if (typPolecenia == TypPolecenia.UPDATE)
                {
                    if (!firstEntry)
                    {
                        query += ", ";
                        selectQuery += " OR ";
                        logParameters += ",\r\n";
                    }
                    firstEntry = false;
                    query += name + "=?";
                    selectQuery += "(" + name + "<>?)";
                    logParameters += "SET " + name + "=" + toLogString(value);
                    parameters.Add(new ValueWithOleDbType(value, oleDbType));
                }
                else if (typPolecenia == TypPolecenia.INSERT)
                {
                    if (!firstEntry)
                    {
                        query += ", ";
                        values += ", ";
                        logParameters += ",\r\n";
                    }
                    firstEntry = false;
                    query += name;
                    values += "?";
                    logParameters += "SET " + name + "=" + toLogString(value);
                    parameters.Add(new ValueWithOleDbType(value, oleDbType));
                }
                else
                {
                    throw new ApplicationException("Nieprawidłowa składnia polecenia!");
                }
            }

            private string toLogString(object value)
            {
                if (value is string)
                {
                    return "\"" + (value as string).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                }
                return value.ToString();
            }

            protected void AddField(string name)
            {
                throw new NotImplementedException();
            }

            protected void SetFieldSubquery(string name, Tabela subquery)
            {
                throw new NotImplementedException();
            }


            protected void _UPDATE()
            {
                if (typPolecenia != TypPolecenia.NONE)
                    throw new ApplicationException("Nieprawidłowa składnia polecenia!");
                typPolecenia = TypPolecenia.UPDATE;
                query = "UPDATE " + nazwa + " SET ";
                selectQuery = "SELECT COUNT(*) FROM " + nazwa + " WHERE (";
                firstEntry = true;
            }

            protected void _INSERT()
            {
                if (typPolecenia != TypPolecenia.NONE)
                    throw new ApplicationException("Nieprawidłowa składnia polecenia!");
                typPolecenia = TypPolecenia.INSERT;
                query = "INSERT INTO " + nazwa + "(";
                firstEntry = true;
            }

            protected void _DELETE()
            {
                if (typPolecenia != TypPolecenia.NONE)
                    throw new ApplicationException("Nieprawidłowa składnia polecenia!");
                typPolecenia = TypPolecenia.DELETE;
                query = "DELETE FROM " + nazwa;
            }

            protected void _WHERE()
            {
                if (typPolecenia == TypPolecenia.NONE || typPolecenia == TypPolecenia.INSERT || where)
                    throw new ApplicationException("Nieprawidłowa składnia polecenia!");
                query += " WHERE ";
                selectQuery += ") AND (";
                logParameters += ",\r\n";
                where = true;
                firstEntry = true;
            }

            public bool EXECUTE(bool returnResult = false)
            {
                OleDbCommand cmd;
                CloseQuery();
                bool skipDbLog = false;
                int updatedRows = -1;
                if (typPolecenia == TypPolecenia.UPDATE)
                {
                    try
                    {
                        selectQuery += ")";
                        cmd = baza.UtworzProstePolecenie(selectQuery);
                        addParameters(cmd);
                        updatedRows = (int)cmd.ExecuteScalar();
                        skipDbLog = (updatedRows == 0);
                    }
                    catch (Exception ex)
                    {
                        DatabaseLog.log(false, baza, "Błąd wykonywania polecenia SELECT sprawdzającego zmiany w poleceniu UPDATE: " + ex.Message, selectQuery, logParameters);
                    }
                }

                try
                {
                    cmd = baza.UtworzProstePolecenie(query);
                    addParameters(cmd);
                    affectedRows = cmd.ExecuteNonQuery();

                    if (typPolecenia == TypPolecenia.INSERT)
                    {
                        try
                        {
                            cmd = baza.UtworzProstePolecenie("SELECT @@Identity");
                            lastId = (long)cmd.ExecuteScalar();
                            lastIdValid = true;
                        }
                        catch (Exception)
                        {
                            lastId = -1;
                            lastIdValid = false;
                        }
                    }

                    if (typPolecenia == TypPolecenia.DELETE)
                    {
                        skipDbLog = (affectedRows == 0);
                    }

                    logParameters += String.Format(",\r\n[AFFECTED ROWS]={0}", affectedRows);
                    if (typPolecenia == TypPolecenia.INSERT)
                        logParameters += String.Format(",\r\n[LAST ID]={0}", lastIdValid);
                    if (updatedRows >= 0)
                        logParameters += String.Format(",\r\n[CHANGED ROWS]={0}", updatedRows);
                    DatabaseLog.log(skipDbLog, baza, logInfo, query, logParameters);
                }
                catch (Exception ex)
                {
                    DatabaseLog.log(false, baza, "Błąd wykonywania polecenia: " + ex.Message, query, logParameters);
                    if (returnResult)
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }

                return true;
            }

            private void addParameters(OleDbCommand cmd)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    OleDbParameter param;
                    var obj = parameters[i];
                    if (obj.value is byte[])
                    {
                        param = cmd.Parameters.Add("a" + i, obj.oleDbType, ((byte[])obj.value).Length);
                    }
                    else if (obj.value is string)
                    {
                        param = cmd.Parameters.Add("a" + i, obj.oleDbType, ((string)obj.value).Length);
                    }
                    else
                    {
                        param = cmd.Parameters.Add("a" + i, obj.oleDbType);
                    }
                    param.Value = obj.value;
                    param.Direction = ParameterDirection.Input;
                }
            }
                       
            protected void _INFO(string text)
            {
                logInfo = text;
            }

            private void CloseQuery()
            {
                if (typPolecenia == TypPolecenia.INSERT && values.Length > 0)
                {
                    query += ") VALUES (" + values + ")";
                    values = "";
                }
            }

            public int AffectedRows { get { return affectedRows; } }
            public long LastId { get { if (!lastIdValid) throw new ApplicationException("Last row ID is invalid!"); return lastId; } }
        }

    }
}
