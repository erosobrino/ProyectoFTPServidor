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
        List<string> rutasDirectorios = new List<string>();

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

            p.cargaFicheros();
            if (!p.compruebaBD())
                Console.WriteLine("No se ha podido establecer conexion con la base de datos\n");
            else
            {
                Console.WriteLine("Conexion correcta con la base de datos\n");
                p.iniciaServidorArchivos();
            }
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
                    hilo.Start();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("El puerto esta ocupado");
            }
        }

        private void hiloCliente(Socket sCliente)
        {

            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw = null;
            bool stop = false;
            bool valido = false;
            string msg = "";
            string rutaActual = ruta;
            try
            {
                using (ns = new NetworkStream(sCliente))
                {
                    using (sr = new StreamReader(ns))
                    {
                        using (sw = new StreamWriter(ns))
                        {
                            sw.WriteLine("Conectado");
                            sw.Flush();
                            string userPass = sr.ReadLine(); //user=admin pass=admin
                            if (userPass != null && userPass != "")
                            {
                                userPass = userPass.Trim();
                                int longitud = userPass.IndexOf(' ') - userPass.IndexOf('=') - 1;
                                if (longitud > 0)
                                {
                                    string usuario = userPass.Substring(userPass.IndexOf('=') + 1, longitud);
                                    string contraseña = userPass.Substring(userPass.LastIndexOf('=') + 1);
                                    valido = usuarioValido(usuario, contraseña);
                                    if (!valido)
                                    {
                                        sw.WriteLine("invalido");
                                        sw.Flush();
                                    }
                                    else
                                    {
                                        sw.WriteLine("valido");
                                        sw.Flush();
                                        while (msg != null && !stop)
                                        {
                                            msg = sr.ReadLine();
                                            if (msg != null)
                                            {
                                                string[] msgSeparado = msg.Split(' ');
                                                switch (msgSeparado[0].ToUpper())
                                                {
                                                    case "LISTADO":
                                                        sw.WriteLine(carpetaActual(rutaActual));
                                                        sw.Flush();
                                                        break;
                                                    case "DIRECTORIO":
                                                        if (rutasDirectorios.Contains((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower()))
                                                        {
                                                            rutaActual += "\\" + msgSeparado[1].Trim();
                                                            sw.WriteLine("Valido");
                                                        }
                                                        else
                                                            sw.WriteLine("Invalido");
                                                        sw.Flush();
                                                        break;
                                                    case "FICHERO":
                                                        if (rutasFicheros.Contains((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower()))
                                                        {
                                                            try
                                                            {
                                                                long ti = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                                                byte[] bytes = File.ReadAllBytes((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower());
                                                                foreach (byte b in bytes)
                                                                {
                                                                    sw.Write(b);
                                                                    Console.Write(b);
                                                                }
                                                                sw.Flush();
                                                                Console.WriteLine(System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - ti);
                                                            }
                                                            catch
                                                            {
                                                                Console.WriteLine("Error al enviar un fichero");
                                                                sw.WriteLine("Error al enviar el fichero");
                                                                sw.Flush();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            sw.WriteLine("No existe el fichero");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "SALIR":
                                                        stop = true;
                                                        break;
                                                    case "CERRAR":
                                                        Environment.Exit(1);
                                                        break;
                                                    case "ATRAS":
                                                        if (rutaActual != ruta)
                                                        {
                                                            rutaActual = rutaActual.Substring(0, rutaActual.LastIndexOf("\\"));
                                                        }
                                                        else
                                                        {
                                                            sw.WriteLine("Ya estas en la raiz");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "USUARIO":
                                                        if (msgSeparado.Length == 3)
                                                        {
                                                            bool modificado = cambiaContraseñaBD(msgSeparado[1], msgSeparado[2]);
                                                            if (modificado)
                                                            {
                                                                sw.WriteLine("valido");
                                                            }
                                                            else
                                                                sw.WriteLine("invalido");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "ADMIN":
                                                        if (msgSeparado.Length == 2)
                                                        {
                                                            bool esAdmin = compruebaAdmin(msgSeparado[1]);
                                                            if (esAdmin)
                                                            {
                                                                sw.WriteLine("valido");
                                                            }
                                                            else
                                                                sw.WriteLine("invalido");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("invalido");
                                    sw.Flush();
                                }
                            }
                        }
                    }
                    sCliente.Close();
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Usuario desconectado");
                sCliente.Close();
            }
        }

        private bool compruebaAdmin(string nombre)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string queryComprueba = "select * from usuarios where nombre='" + nombre + "' and esAdmin=true";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(queryComprueba, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool cambiaContraseñaBD(string nombre, string contraseña)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string query = "UPDATE usuarios SET contraseña='" + contraseña + "' WHERE nombre='" + nombre + "'";
                string queryComprueba = "select contraseña from usuarios where nombre='" + nombre + "' and contraseña='" + contraseña + "'";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    commandDatabase.ExecuteNonQuery();
                    commandDatabase = new MySqlCommand(queryComprueba, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private string carpetaActual(string ruta)
        {
            String nombres = "";
            DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
            DirectoryInfo[] directorios = directoryInfo.GetDirectories();
            foreach (DirectoryInfo directorio in directorios)
            {
                nombres += "D:" + directorio.Name + '?';
            }
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                nombres += "F:" + fileInfo.Name + '?';
            }
            return nombres;
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
                    rutasFicheros.Add(fileInfo.FullName.ToLower());
                }
                Console.WriteLine();
                DirectoryInfo[] directorios = directoryInfo.GetDirectories();
                foreach (DirectoryInfo directorio in directorios)
                {
                    rutasDirectorios.Add(directorio.FullName.ToLower());
                    compruebaDirectorios(directorio.FullName, indice);
                }
            }
            catch
            {
                Console.WriteLine("Ruta no valida");
            }
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
                                        Console.WriteLine("El puerto para el servidor debe ser un numero entre " + IPEndPoint.MinPort + " y " + IPEndPoint.MaxPort);
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
                string conexion = "Server=" + ip + ";port=" + puertoBD + ";Database=mysql;User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
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
                            "nombre CHAR(20) NOT NULL unique," +
                            "contraseña varchar(50) NOT NULL," +
                            " esAdmin BOOL DEFAULT FALSE);";
                        string añadeAdmin = "insert into usuarios (nombre, contraseña, esAdmin) VALUES ('admin', 'admin',true);";

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
                        Console.WriteLine("Se ha creado la base de datos " + nombreBDUsuarios + " para almacenar los usuarios");
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
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
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
