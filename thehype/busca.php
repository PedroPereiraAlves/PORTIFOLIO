<?php
include("conexao.php");

$Nome = $_POST["nome"];
$preco = $_POST["preco"];
$quantidade = $_POST["quantidade"];
$fornecedor = $_POST["fornecedor"];

$sql = "SELECT * from produto WHERE nome LIKE '%".$Nome."%'  OR nome LIKE '%".$Nome."%' ";
$query = $mysqli->query($sql);
$result = mysqli_num_rows($query);

if($query >= "1") {
    echo "Exibindo ".$result." resultados para <strong>".$Nome."</strong><br><br>";
    while($linha = mysqli_fetch_array($query)) {
        $resultado = $linha["resultado"];
        echo $nome."<br>";
    }
   
} else {
    echo "Não foi encontrado nenhum resultado para <strong>".$Nome."</strong>";
}

?>