# Proyecto Servidor FTP
El proyecto consiste en un servidor FTP al cual nos podremos conectar mediante una aplicación para Windows y Android, estas estarán desarrolladas en Xamarin y estará pronto disponibles.
Enlace del cliente: https://github.com/erosobrino/ClienteFTP


### Instalación 

Para poder utilizar este programa primero deberás tener acceso a una base de datos MYSQL y después deberás compilar el codigo de este repositorio.
Una vez hecho esto, puedes añadir nuevos usuarios as servidor mediante un gestor de base de datos como HeidiSQL.


## Realizado con 

* [Visual Studio 2017](https://visualstudio.microsoft.com/es/downloads/) - IDE
* [Conector MySQL/NET](https://dev.mysql.com/downloads/connector/net/) - Conector para hacer uso de bases de datos MySQL desde C#
* [XAMPP](https://www.apachefriends.org/es/index.html) - Sistema gestor de bases de datos.


## Avances

12/04/2019 Crear el proyecto<br>
19/04/2019 Metodos para leer configuracion, conectar bd o crear para tabla de usuarios, comprobacion de usuario valido y carga de ficheros<br>
23/04/2019 Metodo para lanzar hilos para conexiones, añadir nuevo elemento al fichero de configuracion y correccion en conexion a bd<br>
27/04/2019 Funcion hiloCLiente, comprueba usuario, muestra ficheros de la ruta de ese cliente, permite cambiar de directorio y salir<br>
01/05/2019 Crear proyecto Xamarin para el cliente y añadir pantalla principal en ClienteFTP<br>
04/05/2019 Pantalla de inicio, pantalla de listado con botones y visualizacion de listado con imagenes en ClienteFTP<br>
