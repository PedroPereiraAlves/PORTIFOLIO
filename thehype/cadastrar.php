<?php

// INCLUINDO O ARQUIVO DE CONEXAO COM O BANCO DE DADOS

include "conexao.php";
// RECEBENDO TODOS DO VALORES DO FORMULÁRIO FORMCADASTRAPRODUTO.PHP

// CADA VARIÁVEL RECEBE UM VALOR INFORMADO PELOS CAMPOS DO FORMULÁRIO

$nome = $_POST["nome"];

$nascimento = $_POST["nascimento"];

$cpf = $_POST["cpf"];

$tel = $_POST["tel"];

$email = $_POST["email"];

$senha = $_POST["senha"];

$sql = "INSERT INTO cliente ( CPF , Nome , Email , senha , numero_telefone , nascimento )
			VALUES ( '$cpf' , '$nome', '$email', '$senha', '$numero_telefone' , '$nascimento')";
$query = $mysqli->query($sql);
		
$mysqli->close();
echo "Guardado com sucesso<br />";

?>