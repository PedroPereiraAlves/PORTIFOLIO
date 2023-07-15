<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<br><br>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title></title>

</head>

<body>

<H2>LOCALIZAR PRODUTOS</H2>
<div class="form">
<form method="post" action="listar.php">

Selecione a forma de busca:

<select name="cbocampo">
<option value="nome">Nome</option>
<option value="fornecedor">Fornecedor</option>
</select>


<br> <br> Faça a busca: <input type="text" name="txtparametro" size="25">

<input type="submit" value="Buscar Produto">

</form>
</div>

<div id="logo">
<img width="300" height="144" src="img/logo.png"/>
</div>
</body>

</html>

<style>
@import url(http://fonts.googleapis.com/css?family=Montserrat:400,700);

body {
	width: 1383px;
	max-width: 100%;
	overflow-x: hidden;
    overflow-y: hidden;
	height: 1200;
	font-family: Montserrat;
	margin: 0 auto;
}


h2 {
text-align: center;
}

.form { 
        width: 500px; 
		padding: 40px 20px; 
		position: relative;
		left: 30%; 
		top: -1000;
		font-family: Montserrat,arial,serif;
		color: #000;
		margin-top: 1000; 

		}


input{width: 100%; 
		background: none; 
		border: 1px solid #000; 
		border-radius: 3px; 
		padding: 6px 15px; 
		box-sizing: border-box;
		margin-bottom: 20px; 
		font-size: 16px; 
		color: #000; 
		font-family: Montserrat,arial,serif;}
        
        input[type="submit"]{ background: #000; border: 0; cursor: pointer; color: #fff; font-family: Montserrat,arial,serif;}
        input[type="submit"]:hover{ background: gray; transition: .6s;}
		
		#logo {
			position: relative;
			top: -1000;
			left: 39%;
</style>