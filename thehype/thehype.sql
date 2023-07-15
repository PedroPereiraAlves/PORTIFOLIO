-- phpMyAdmin SQL Dump
-- version 4.9.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Tempo de geração: 21-Nov-2019 às 03:18
-- Versão do servidor: 10.4.8-MariaDB
-- versão do PHP: 7.1.33

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Banco de dados: `thehype`
--

-- --------------------------------------------------------

--
-- Estrutura da tabela `cliente`
--

CREATE TABLE `cliente` (
  `CPF` int(50) NOT NULL,
  `Nome` varchar(50) NOT NULL,
  `Email` varchar(50) NOT NULL,
  `senha` varchar(20) NOT NULL,
  `numero_telefone` varchar(50) NOT NULL,
  `nascimento` varchar(11) DEFAULT NULL,
  `admin` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Extraindo dados da tabela `cliente`
--

INSERT INTO `cliente` (`CPF`, `Nome`, `Email`, `senha`, `numero_telefone`, `nascimento`, `admin`) VALUES
(2147483647, 'Pedro Pereira Alves', 'pedro05vr@gmail.com', '15072008', '', '2001-07-18', 1);

-- --------------------------------------------------------

--
-- Estrutura da tabela `fornecedor`
--

CREATE TABLE `fornecedor` (
  `Cod_fornecedor` int(10) DEFAULT NULL,
  `Nome` varchar(30) DEFAULT NULL,
  `Numero_Telefone` varchar(30) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura da tabela `itens_pedido`
--

CREATE TABLE `itens_pedido` (
  `id` int(20) NOT NULL,
  `Cod_pedido` int(20) DEFAULT NULL,
  `Cod_produto` int(20) DEFAULT NULL,
  `Quantidade` varchar(50) DEFAULT NULL,
  `estatus` varchar(20) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura da tabela `login`
--

CREATE TABLE `login` (
  `senha` varchar(30) NOT NULL,
  `Email` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura da tabela `pagamento`
--

CREATE TABLE `pagamento` (
  `Cod_pagamento` int(20) NOT NULL DEFAULT 0,
  `Nome` varchar(50) DEFAULT NULL,
  `Email` varchar(50) DEFAULT NULL,
  `Endereco` varchar(50) DEFAULT NULL,
  `Estado` varchar(50) DEFAULT NULL,
  `CEP` varchar(10) DEFAULT NULL,
  `Nome_cartao` varchar(50) DEFAULT NULL,
  `Num_cartao` varchar(50) DEFAULT NULL,
  `Mes_esp` varchar(10) DEFAULT NULL,
  `ano_esp` varchar(10) DEFAULT NULL,
  `CVV` varchar(20) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura da tabela `pedido`
--

CREATE TABLE `pedido` (
  `cod_pedido` int(10) NOT NULL,
  `estatus` varchar(10) DEFAULT NULL,
  `form_pag` varchar(10) DEFAULT NULL,
  `cod_produto` int(10) DEFAULT NULL,
  `emissao` date DEFAULT NULL,
  `CPF` int(20) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Estrutura da tabela `produto`
--

CREATE TABLE `produto` (
  `Cod_produto` int(50) NOT NULL,
  `fornecedor` varchar(50) NOT NULL,
  `quantidade` varchar(50) NOT NULL,
  `nome` varchar(50) NOT NULL,
  `preco` double(6,2) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Extraindo dados da tabela `produto`
--

INSERT INTO `produto` (`Cod_produto`, `fornecedor`, `quantidade`, `nome`, `preco`) VALUES
(1, 'pedro', '0', 'camisa', 15.00),
(3, 'pedro', '2', 'bermuda', 20.00),
(4, 'a', '1', 'bone', 200.00);

--
-- Índices para tabelas despejadas
--

--
-- Índices para tabela `cliente`
--
ALTER TABLE `cliente`
  ADD PRIMARY KEY (`CPF`);

--
-- Índices para tabela `itens_pedido`
--
ALTER TABLE `itens_pedido`
  ADD PRIMARY KEY (`id`),
  ADD KEY `Cod_pedido` (`Cod_pedido`),
  ADD KEY `Cod_produto` (`Cod_produto`);

--
-- Índices para tabela `pagamento`
--
ALTER TABLE `pagamento`
  ADD PRIMARY KEY (`Cod_pagamento`);

--
-- Índices para tabela `pedido`
--
ALTER TABLE `pedido`
  ADD PRIMARY KEY (`cod_pedido`),
  ADD KEY `cod_produto` (`cod_produto`),
  ADD KEY `CPF` (`CPF`);

--
-- Índices para tabela `produto`
--
ALTER TABLE `produto`
  ADD PRIMARY KEY (`Cod_produto`);

--
-- AUTO_INCREMENT de tabelas despejadas
--

--
-- AUTO_INCREMENT de tabela `produto`
--
ALTER TABLE `produto`
  MODIFY `Cod_produto` int(50) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Restrições para despejos de tabelas
--

--
-- Limitadores para a tabela `itens_pedido`
--
ALTER TABLE `itens_pedido`
  ADD CONSTRAINT `Cod_pedido` FOREIGN KEY (`Cod_pedido`) REFERENCES `pedido` (`cod_pedido`);

--
-- Limitadores para a tabela `pedido`
--
ALTER TABLE `pedido`
  ADD CONSTRAINT `CPF` FOREIGN KEY (`CPF`) REFERENCES `cliente` (`CPF`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
