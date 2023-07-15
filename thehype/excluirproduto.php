

<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title>Controle de Produtos</title>

</head>

<body>

<?

// INCLUINDO O ARQUIVO DE CONEXÃO COM O BANCO

include 'conexao.php';

// RECEBENDO A RESPOSTA DO USUÁRIO

$resposta = $_POST["optresposta"];

// RECEBENDO O ID DO NOME A SER EXCLUÍDO

$nome = $_POST["txtnome"];

// VERIFICANDO A RESPOSTA DO USUÁRIO

if ($resposta == "N")

// CASO A OPÇÃO SELECIONADA NO FORMEXCLUIR PRODUTO SEJA "NÃO",

// O MESMO SERÁ REDIRECIONADO PARA A PÁGINA menuproduto.php

header("Location: menuproduto.php");

else

{

// QUERY COM O COMANDO DE EXCLUSÃO

$query = "DELETE FROM produto WHERE nome = $nome";

// EXECUTAR A EXCLUSÃO NO BANCO DE DADOS

mysql_query($query) or die(mysql_error());

echo "<P>O produto foi excluído com sucesso.</P>";

}

// ENCERRANDO A CONEXÃO COM O BANCO

mysql_close();

?>

</body>

</html>