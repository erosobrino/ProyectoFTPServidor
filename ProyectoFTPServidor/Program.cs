using System;
using MySql.Data.MySqlClient;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace ProyectoFTPServidor
{
    class Program
    {
        IPAddress ip;
        string puertoBD;
        int puertoServer;
        string usuarioBD;
        string contraseñaBD;
        string nombreBDUsuarios;
        string ruta;
        string rutaConfig = Environment.GetEnvironmentVariable("homedrive") + "\\" + Environment.GetEnvironmentVariable("homepath") + "\\configServidorFTP.txt";
        List<string> rutasFicheros = new List<string>();

        static void Main(string[] args)
        {
            Program p = new Program();
            p.leeConfiguracion();

            Console.WriteLine("Puedes modificar el archivo de configuracion en " + p.rutaConfig);
            Console.WriteLine("IP: " + p.ip);
            Console.WriteLine("Usuario: " + p.usuarioBD);
            Console.WriteLine("Contraseña: " + p.contraseñaBD);
            Console.WriteLine("Base de datos: " + p.nombreBDUsuarios);
            Console.WriteLine("Puerto BD: " + p.puertoBD);
            Console.WriteLine("Puerto Servidor: " + p.puertoServer);
            Console.WriteLine("Ruta: " + p.ruta + "\n");

            if (p.compruebaBD())
                Console.WriteLine("Conexion correcta con la base de datos\n");
            else
                Console.WriteLine("No se ha podido establecer conexion con la base de datos\n");

            p.cargaFicheros();

            Console.WriteLine(p.usuarioValido("admin", "admin"));

            Console.ReadLine();
        }

        public void iniciaServidorArchivos()
        {
            try
            {
                IPEndPoint ie = new IPEndPoint(IPAddress.Any, puertoServer);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                s.Bind(ie);
                s.Listen(10);
                Console.WriteLine("Escuchando en el puerto " + puertoServer);
                while (true)
                {
                    Socket sCliente = s.Accept();
                    Thread hilo = new Thread(() => hiloCliente(sCliente));
                    hilo.IsBackground = true;
                    hilo.Start(sCliente);
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("El puerto esta ocupado");
            }
            Console.ReadKey();
        }

        private void hiloCliente(Socket sCliente)
        {

        }

        private void cargaFicheros()
        {
            if (Directory.Exists(ruta))
            {
                compruebaDirectorios(ruta, 0);
            }
            else
            {
                Console.WriteLine("Debes indicar una ruta en el fichero de configuracion " + rutaConfig);
            }
        }

        private void compruebaDirectorios(string ruta, int indice)
        {
            try
            {
                indice++;
                DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                for (int i = 0; i < indice - 1; i++)
                {
                    Console.Write("\t");
                }
                Console.WriteLine(directoryInfo.Name);
                foreach (FileInfo fileInfo in fileInfos)
                {
                    for (int j = 0; j < indice; j++)
                    {
                        Console.Write("\t");
                    }
                    Console.WriteLine(fileInfo.Name);
                    rutasFicheros.Add(fileInfo.FullName);
                }
                Console.WriteLine();
                DirectoryInfo[] directorios = directoryInfo.GetDirectories();
                foreach (DirectoryInfo directorio in directorios)
                {
                    compruebaDirectorios(directorio.FullName, indice);
                }
            }
            catch { }
        }

        private void leeConfiguracion()
        {
            if (File.Exists(rutaConfig))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(rutaConfig))
                    {
                        string lineaActual = sr.ReadLine();
                        while (lineaActual != null)
                        {
                            switch (lineaActual.Substring(0, lineaActual.IndexOf('=')).ToLower())
                            {
                                case "ip":
                                    ip = IPAddress.Parse(lineaActual.Substring(lineaActual.IndexOf('=') + 1));
                                    break;
                                case "usuariobd":
                                    usuarioBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "contraseñabd":
                                    contraseñaBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "nombrebdusuarios":
                                    nombreBDUsuarios = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "puertobd":
                                    puertoBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "ruta":
                                    ruta = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "puertoserver":
                                    try
                                    {
                                        puertoServer = Convert.ToInt32(lineaActual.Substring(lineaActual.IndexOf('=') + 1));
                                        if (puertoServer < IPEndPoint.MinPort || puertoServer > IPEndPoint.MaxPort)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("El puerto para el servidor debe ser un numero entre "+ IPEndPoint.MinPort+" y "+IPEndPoint.MaxPort);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            lineaActual = sr.ReadLine();
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error leyendo el archivo de configuracion");
                }
            }
            else
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(rutaConfig))
                    {
                        sw.WriteLine("ip=127.0.0.1");
                        sw.WriteLine("usuarioBD=root");
                        sw.WriteLine("contraseñaBD=");
                        sw.WriteLine("nombreBDUsuarios=UsuariosFTP");
                        sw.WriteLine("puertoBD=3306");
                        sw.WriteLine("ruta=");
                        sw.WriteLine("puertoServer=31416");
                    }
                    leeConfiguracion();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error creando el archivo de configuracion");
                }
            }
        }

        private bool compruebaBD()
        {
            try
            {
                //Comprueba que existe la base de datos y sino la crea con el usuario admin por defecto
                string conexion = "Server=" + ip + ";port="+puertoBD+";Database=mysql;User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string query = "SHOW DATABASES LIKE '" + nombreBDUsuarios + "'";

                using (MySqlConnection con = new MySqlConnection(conexion))
                {
                    MySqlCommand commandDatabase;
                    commandDatabase = new MySqlCommand(query, con);
                    MySqlDataReader reader;
                    con.Open();
                    reader = commandDatabase.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        reader.Close();
                        Console.WriteLine("No hay bd");
                        string creaBD = "CREATE DATABASE " + nombreBDUsuarios + " CHARACTER SET UTF8 COLLATE UTF8_SPANISH_CI;";
                        string creaTabla = "CREATE TABLE usuarios (id INT AUTO_INCREMENT PRIMARY KEY," +
                            "nombre CHAR(20) NOT NULL," +
                            "contraseña varchar(50));";
                        string añadeAdmin = "insert into usuarios (nombre, contraseña) VALUES ('admin', 'admin');";

                        commandDatabase = new MySqlCommand(creaBD, con);
                        commandDatabase.ExecuteNonQuery();

                        string conexionBDUsuarios = "Server=" + ip + ";Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                        using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                        {
                            conBDUsuarios.Open();
                            commandDatabase = new MySqlCommand(creaTabla, conBDUsuarios);
                            commandDatabase.ExecuteNonQuery();
                            commandDatabase = new MySqlCommand(añadeAdmin, conBDUsuarios);
                            commandDatabase.ExecuteNonQuery();
                        }
                        Console.WriteLine("addfsdf");
                    }
                }
                return true;
            }
            catch (MySqlException)
            {
                return false;
            }
        }

        private bool usuarioValido(string nombre, string contraseña)
        {
            try
            {
                string query = "SELECT * FROM usuarios WHERE nombre = '" + nombre + "' AND contraseña = '" + contraseña + "'";
                string conexionBDUsuarios = "Server=" + ip + ";Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch (MySqlException)
            {
                Console.WriteLine("Error al comprobar usuario");
                return false;
            }
        }
    }
}
