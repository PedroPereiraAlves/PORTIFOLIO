<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title></title>

</head>

<body>

<?php

// INCLUINDO O ARQUIVO DE CONEXAO COM O BANCO DE DADOS

include "conexao.php";

// RECEBENDO TODOS DO VALORES DO FORMULÁRIO FORMCADASTRARPRODUTO.PHP

// CADA VARIÁVEL RECEBE UM VALOR INFORMADO PELOS CAMPOS DO FORMULÁRIO

$id = $_POST['txtid'];

$nome = $_POST["txtnome"];

$preco = $_POST["txtpreco"];

$fornecedor = $_POST["txtfornecedor"];

$quantidade= $_POST["txtquantidade"];

// VARIÁVEL QUE RECEBE O COMANDO DE ALTERAÇÃO DE ALUNO

$query = "update produto set nome='$nome', preco='$preco',fornecedor='$fornecedor', quantidade='$quantidade',";

// EXECUTAR A ALTERAÇÃO DO PRODUTO NO BANCO

mysql_query($query) or die(mysql_error());

// VERIFICANDO SE O ALUNO FOI ALTERADO

if(mysql_affected_rows() == 1)

{

echo "O Produto : $nome foi Alterado com Sucesso!";

}

else

{

echo "Erro ao Atualizar os Dados do Produto!";

}

// ENCERRA A CONEXÃO COM BANCO

mysql_close();

?>

</body>

</html>