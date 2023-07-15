<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">

<head>

<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />

<title>Cadastro de Produto</title>

</head>

<body>


<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";
?>

<H2>CADASTRO DE PRODUTOS</H2>

<div class="form">
<form method="post" action="salvarproduto.php">

<table width="auto" border="0">

<tr>

<td>Nome</td>

<td><input type="text" name="txtnome" id="txtnome" size="45"/></td>

</tr>

<tr>

<td>Preço</td>

<td><input type="text" name="txtpreço" id="txtpreço" /></td>

</tr>

<tr>

<td>Fornecedor</td>

<td><input type="text" name="txtfornecedor" id="txtfornecedor" /></td>

</tr>

<tr>

<td>Quantidade</td>

<td><input type="text" name="txtquantidade" id="txtquantidade" /></td>

</tr>


</tr>

<tr>

<td></td>

<td><input type="submit" value="Cadastrar" />

<input type="reset" value="Cancelar" /></td>

</tr>

</table>

</form>
</div>
<br>

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
		left: 27%; 
		top: 20%;
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
			left: 39%;
</style>