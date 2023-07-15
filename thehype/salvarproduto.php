<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<?php

// INCLUINDO O ARQUIVO DE CONEXAO COM O BANCO DE DADOS

include "conexao.php";
// RECEBENDO TODOS DO VALORES DO FORMULÁRIO FORMCADASTRAPRODUTO.PHP

// CADA VARIÁVEL RECEBE UM VALOR INFORMADO PELOS CAMPOS DO FORMULÁRIO

$nome = $_POST["txtnome"];

$preco = $_POST["txtpreço"];

$Fornecedor = $_POST["txtfornecedor"];

$Quantidade = $_POST["txtquantidade"];

$sql = "INSERT INTO produto ( Cod_produto , nome , preco , fornecedor , quantidade )
			VALUES ( NULL , '$nome', '$preco', '$Fornecedor', '$Quantidade')";
$query = $mysqli->query($sql);
		
$mysqli->close();
echo "Guardado com sucesso<br />";

?>