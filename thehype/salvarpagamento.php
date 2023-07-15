<?php

// INCLUINDO O ARQUIVO DE CONEXAO COM O BANCO DE DADOS

include "conexao.php";
// RECEBENDO TODOS DO VALORES DO FORMULÁRIO FORMCADASTRAPRODUTO.PHP

// CADA VARIÁVEL RECEBE UM VALOR INFORMADO PELOS CAMPOS DO FORMULÁRIO

$nome = $_POST["txtnome"];

$email = $_POST["txtemail"];

$endereco = $_POST["txtendereco"];

$cidade = $_POST["txtcidade"];

$estado= $_POST["txtestado"];

$CEP = $_POST["txtCEP"];

$cardname = $_POST["txtnomecartao"];

$cardnumber = $_POST["txtnumerocartao"];

$mes_esp = $_POST["expmonth"];

$ano_esp = $_POST["expyear"];

$CVV = $_POST["txtcvv"];

$sql = "INSERT INTO pagamento ( Cod_pagamento , Nome , Email  , Endereco , CEP , Nome_cartao , Num_cartao , Mes_esp , ano_esp , CVV )
			VALUES ( NULL , '$nome', '$email', '$endereco', '$CEP', '$Nome_cartao', '$Num_cartao', '$Mes_esp', '$ano_esp', '$CVV')";
$query = $mysqli->query($sql);
		
$mysqli->close();
echo "Guardado com sucesso<br />";

?>